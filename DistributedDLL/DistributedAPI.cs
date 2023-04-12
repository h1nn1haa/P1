using System.IO.Ports;
using static System.Net.Mime.MediaTypeNames;

namespace DistributedDLL
{
    class Packet
    {
        byte[] headerSync = { 0xAA, 0x01, 0x02 };
        byte packetType = 0x01;
        byte paylaodSize = 0x01;
        byte packetID = 0x00;
        byte payload = 0x00;
        byte checkSum;
    }

    // manage watchdog and reconnection automatically
    // send specific packet to reset wdg_timer...but device will drop out anyways so we have to reconnect
    // connect function should send packet and return response to app
    // firmware wdg will reset if packet is not received every 8s
    // firmware will also reset every 60s no matter what
    // goals: 1. do not let wdg go hungry, 2. detect and reconnect after disconnects
    // send packet and receive response which confirms that it was received...maintain connection by starting up a timer and then 
    // regularly send a wdg reset which will keep session alive
    // don't let wdg go hungry but also reestablish connections on interval basis when device crashes out
    public class DistributedAPI
    {
        public bool isConnected = true;
        SerialPort _serialPort;
        byte PAYLOAD_SIZE = 0x01;
        public DistributedAPI()
        {
            _serialPort = new SerialPort();

            _serialPort.BaudRate = 115200;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Parity = Parity.None;

            //timeouts
            //_serialPort.ReadTimeout = 500;
            //_serialPort.WriteTimeout = 500;
        }

        private byte calcCheckSum(byte packetID, byte packetType, byte payloadSize, byte[] payload)
        {
            int checksum = 0xAD + packetID + packetType + payloadSize;
            for (int i = 0; i < payloadSize; i++)
            {
                checksum += payload[i];
            }

            return (byte)checksum;
        }

        private bool isPacketAccepted(byte[] packetSent, byte[] packetReceived)
        {
            return packetSent.SequenceEqual(packetReceived);
        }

        private byte[] createPacket(byte packetID, byte packetType, byte[] payload)
        {
            byte[] HEADER_SYNC = { 0xAA, 0x01, 0x02 };
            byte PAYLOAD_SIZE = (byte)payload.Length;
            int PACKET_SIZE = 7 + PAYLOAD_SIZE;
            byte[] packet = new byte[PACKET_SIZE];

            for (int i = 0; i < 3; i++)
            {
                packet[i] = HEADER_SYNC[i];
            }

            packet[3] = packetType;
            packet[4] = packetID;
            packet[5] = PAYLOAD_SIZE;

            for (int i = PAYLOAD_SIZE; i > 0; i--)
            {
                packet[PACKET_SIZE - i - 1] = payload[PACKET_SIZE - i - 7];
            }

            packet[PACKET_SIZE - 1] = calcCheckSum(packetID, packetType, PAYLOAD_SIZE, payload);

            return packet;
        }

        /*
         * 
         * sent 170, 1, 2, 1, 1, 1, 1, 179
before read
after read
received 170, 1, 2, 0, 1, 1, 0, 175
         * */
        private (bool, string?) checkIfPacketFailed(byte[] packet)
        {
            if (packet[3] == 0x00)
            {
                if (packet[6] == 0x00)
                {
                    return (true, "incorrect checksum");
                }
                else // if (packet[6] == 0x01)
                {
                    return (true, "payload length too long");
                }
            }
            return (false, null);
        }
        
        public ConnectStatus Connect(string port)
        {
            #region
            /*
            byte[] HEADER_SYNC = { 0xAA, 0x01, 0x02 };
            byte PACKET_ID = 0x01;
            byte PACKET_TYPE = 0x01;
            byte[] PAYLOAD = { 0x01 };
            byte PAYLOAD_SIZE = (byte)PAYLOAD.Length;
            int PACKET_SIZE = 7 + PAYLOAD_SIZE;
            byte[] packet = new byte[PACKET_SIZE];

            for (int i = 0; i < 3; i++)
            {
                packet[i] = HEADER_SYNC[i];
            }

            packet[3] = PACKET_TYPE;
            packet[4] = PACKET_ID;
            packet[5] = PAYLOAD_SIZE;

            for (int i = PAYLOAD_SIZE;  i > 0 ; i--)
            {
                packet[PACKET_SIZE - i - 1] = PAYLOAD[PACKET_SIZE - i - 7];
            }

            packet[PACKET_SIZE - 1] = calcCheckSum(PACKET_ID, PACKET_TYPE, PAYLOAD_SIZE, PAYLOAD);
            */
            #endregion

            try
            {
                byte[] payload = { 0x01 };
                byte[] packet = createPacket(0x01, 0x01, payload);

                Console.WriteLine($"Attempting to open serial port {port}");
                _serialPort.PortName = port;
                _serialPort.Open();
                Console.WriteLine($"Successfully opened serial port {port}");


                _serialPort.Write(packet, 0, 8);
                Console.WriteLine($"sent {String.Join(", ", packet)}");
                byte[] readBuffer = new byte[8];
                Console.WriteLine("before read");
                _serialPort.Read(readBuffer, 0, 8);
                Console.WriteLine("after read");
                Console.WriteLine($"received {String.Join(", ", readBuffer)}");
                (bool, string?) isFailedPacket = checkIfPacketFailed(readBuffer);
                if (isFailedPacket.Item1) { Console.WriteLine(isFailedPacket.Item2); }
                _serialPort.Close();
            } catch {
                Console.Write($"failed to open serial port {port}");
            }
            
          

            // header sync - 3 bytes - 0xAA, 0x01, 0x02
            // packet type - 1 byte - failure 0x00, transaction 0x01, streaming 0x02
            // packet id - 1 byte - increment for every packet sent from pc
            // payload size - 1 byte - 0x01
            // payload 1 byte - init_conn 0x01, wdg_reset - 0x02
            // checksum 1 byte

            // received packets will return same packet back if correct, otherwise:
            // packet type - failure 0x00
            // packet id - 0x00
            // payload - bad checksum 0x00, payload length too long 0x01

            // sent
            //170, 1, 2, 1, 1, 1, 1, 177
            //received
            //170, 1, 2, 1, 1, 1, 1, 177

            //sent
            // 170, 1, 2, 1, 2, 1, 1, 178
            //received
            //170, 1, 2, 1, 2, 1, 1, 178

            //sent wrong checksum (supposed to be 170, 1, 2, 1, 1, 1, 2, 177)
            //170, 1, 2, 1, 1, 1, 1, 178
            // received
            //170, 1, 2, 0, 1, 1, 0, 175

            // we should handle case when payload size exceeds max length...couldn't get example

            
            return ConnectStatus.CONN_SUCCESS;
        }
    }
}
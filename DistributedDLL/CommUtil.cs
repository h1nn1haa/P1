using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDLL
{
    public class CommUtil
    {

    }

    public enum ConnectStatus
    {
        CONN_SUCCESS = 1,
        CONN_FAIL = 2
    }

    public enum OpCode
    {
        OP_INIT_CONN = 0x01,
        OP_RESET_WATCHDOG = 0x02
    };

    public enum PacketType
    {
        PKT_FAILURE = 0x00,
        PKT_TRANSACTION = 0x01,
        PKT_STREAM = 0x02
    };

    public enum ERR_CODE
    {
        ERR_BAD_CHECKSUM = 0x00,
        ERR_PAYLOAD_LENGTH_EXCEEDS_MAX = 0x01
    };
}

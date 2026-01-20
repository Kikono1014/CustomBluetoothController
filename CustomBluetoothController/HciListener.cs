using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CustomBluetoothController;

public sealed partial class HciListener : IDisposable
{
    private const int AF_BLUETOOTH = 31;
    private const int SOCK_RAW = 3;
    private const int BTPROTO_HCI = 1;
    private const int SOL_HCI = 0;
    private const int HCI_FILTER = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SockAddrHci { public ushort Family; public ushort Device; public ushort Channel; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HciFilter { public uint TypeMask; public ulong EventMask; public ushort Opcode; }

    internal static partial class NativeMethods
    {
        [LibraryImport("libc", EntryPoint = "socket", SetLastError = true)]
        public static partial int Socket(int domain, int type, int protocol);

        [LibraryImport("libc", EntryPoint = "bind", SetLastError = true)]
        public static partial int Bind(int sockfd, ref SockAddrHci addr, int addrlen);

        [LibraryImport("libc", EntryPoint = "setsockopt", SetLastError = true)]
        public static partial int SetSockOpt(int sockfd, int level, int optname, ref HciFilter optval, int optlen);
    }

    private readonly Socket _socket;

    public HciListener(ushort deviceIndex = 0)
    {
        int fd = NativeMethods.Socket(AF_BLUETOOTH, SOCK_RAW, BTPROTO_HCI);
        if (fd < 0) throw new Exception($"Socket fail: {Marshal.GetLastPInvokeError()}");

        var addr = new SockAddrHci { Family = AF_BLUETOOTH, Device = deviceIndex, Channel = 2 };
        NativeMethods.Bind(fd, ref addr, Marshal.SizeOf(addr));

        var filter = new HciFilter { TypeMask = uint.MaxValue, EventMask = ulong.MaxValue };
        int result = NativeMethods.SetSockOpt(fd, SOL_HCI, HCI_FILTER, ref filter, Marshal.SizeOf(filter));
        
        _socket = new Socket(new SafeSocketHandle((IntPtr)fd, true));
    }

    public async IAsyncEnumerable<byte[]> ListenAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        byte[] buffer = new byte[2048];

        while (!ct.IsCancellationRequested)
        {
            int received;
            try
            {
                received = await _socket.ReceiveAsync(buffer, SocketFlags.None, ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (received > 0)
            {
                byte[] packet = new byte[received];
                Buffer.BlockCopy(buffer, 0, packet, 0, received);
                
                yield return packet;
            }
        }
    }

    public void Dispose() => _socket?.Dispose();
}
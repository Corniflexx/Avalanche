using System.Reflection.PortableExecutable;

namespace Avalanche.Core.Networking.Transmission;

public class PacketBase
{
    #region Propertise

    public ushort Length { get; protected set; }
    public PacketMember Members { get; protected set; }

    #endregion

    #region Fields And Constants

    private SeriLog log;
    private BinaryReader _binaryReader;
    private BinaryWriter _binaryWriter;
    private static bool PacketInitialized;
    private const int BytesPerDumpLine = 16;
    private static Dictionary<PacketMember, Type> PacketMembers { get; } = new();

    #endregion

    #region Constructors

    protected PacketBase()
    {
    }
    public PacketBase(byte[] bytes)
    {
        if (!PacketInitialized)
            PacketInitialization();

        this._binaryReader = new BinaryReader(new MemoryStream(bytes));
    }

    #endregion

    #region Implemention Methods

    public virtual void Read()
    {
    }
    public virtual void Build()
    {
    }

    #endregion

    #region Read Primitive Types

    public float ReadSingle() => this._binaryReader.ReadSingle();
    public byte ReadByte() => this._binaryReader.ReadByte();
    public byte[] ReadBytes(int amount) => this._binaryReader.ReadBytes(amount);
    public short ReadInt16() => this._binaryReader.ReadInt16();
    public ushort ReadUInt16() => this._binaryReader.ReadUInt16();
    public int ReadInt32() => this._binaryReader.ReadInt32();
    public uint ReadUInt32() => this._binaryReader.ReadUInt32();
    public long ReadInt64() => this._binaryReader.ReadInt64();
    public ulong ReadUInt64() => this._binaryReader.ReadUInt64();
    public double ReadDouble() => this._binaryReader.ReadDouble();

    #endregion

    #region Read Strings

    public string ReadString()
    {
        var i = ReadInt16();
        var stringBuffer = ReadBytes(i);
        return Encoding.UTF8.GetString(stringBuffer).Trim('\u0000');
    }
    public string ReadString(int amount)
    {
        var stringBuffer = ReadBytes(amount);
        return Encoding.UTF8.GetString(stringBuffer).Trim('\u0000');
    }

    #endregion

    #region Write Primitive Types

    public void WriteFloat(float value) => this._binaryWriter.Write(value);
    public void WriteInt16(short value) => this._binaryWriter.Write(value);
    public void WriteUInt16(ushort value) => this._binaryWriter.Write(value);
    public void WriteInt32(int value) => this._binaryWriter.Write(value);
    public void WriteUInt32(uint value) => this._binaryWriter.Write(value);
    public void WriteInt64(long value) => this._binaryWriter.Write(value);
    public void WriteUInt64(ulong value) => this._binaryWriter.Write(value);
    public void WriteDouble(double value) => this._binaryWriter.Write(value);

    #endregion

    #region Write Strings

    public void WriteString(string value, int length)
    {
        var array = new byte[length];
        Encoding.ASCII.GetBytes(value).CopyTo(array, 0);
        this._binaryWriter.Write(array);
    }
    public void WriteListStrings(List<string> strings)
    {
        this._binaryWriter.Write((byte)strings.Count);
        for (int i = 0; i < strings.Count; i++)
            this._binaryWriter.Write(strings[i]);
    }

    #endregion

    #region packet Helpers

    private byte[] GetBuffer()
    {
        if (_binaryReader?.BaseStream is MemoryStream memoryStream && memoryStream.Length > 0)
            return memoryStream.ToArray();
        return Array.Empty<byte>();
    }
    protected void Seek(long offset, SeekOrigin seekOrigin)
    {
        this._binaryWriter?.BaseStream?.Seek(offset, seekOrigin);
        this._binaryReader?.BaseStream?.Seek(offset, seekOrigin);
    }
    public string Dump()
    {
        return Dump(GetBuffer());
    }
    public static string Dump(byte[] buffer)
    {
        var lines = (BitConverter.ToUInt16(buffer, 0) + BytesPerDumpLine - 1) / BytesPerDumpLine;
        var size = 72 + 72 + lines * 9 + lines * 3 * BytesPerDumpLine + lines * 1 * BytesPerDumpLine;

        int size1 = BitConverter.ToUInt16(buffer, 0);
        var builder = new StringBuilder(lines);

        // header
        builder.AppendLine("      00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f 0123456789abcdef");
        builder.AppendLine("    +------------------------------------------------ ----------------");
        for (var i = 0; i < size1; i += BytesPerDumpLine)
        {
            builder.Append(i.ToString("x3"));
            builder.Append(" | ");

            // create byte display
            for (var j = i; j < i + BytesPerDumpLine; j++)
            {
                var s = "   ";
                if (j < size1) s = buffer[j].ToString("x2") + " ";

                builder.Append(s);
            }

            builder.Append(' ');

            // create char representation
            for (var j = i; j < i + BytesPerDumpLine; j++)
            {
                var c = ' ';
                if (j < size1)
                {
                    c = (char)buffer[j];
                    if (c < ' ' || c >= 127) c = '.';
                }

                builder.Append(c);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
    #endregion

    #region Packet Operations

    private void PacketInitialization()
    {
        PacketInitialized = true;
        if (PacketMembers.Count != 0)
        {
            return;
        }
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()
               .Where(y => y.IsSubclassOf(typeof(PacketBase)) && !y.IsAbstract));
        foreach (var t in types)
        {
            var packet = (PacketBase)t.CreateInstance();
            if (!PacketMembers.ContainsKey(packet.Members))
                PacketMembers.Add(packet.Members, t);
        }
    }
    public static PacketBase GetPacket(PacketMember members)
    {
        if (PacketMembers.TryGetValue(members, out var type))
        {
            return (PacketBase)type.CreateInstance();
        }
        return null;
    }
    public PacketMember GetPacketMember()
    {
        ReadInt16();
        Members = (PacketMember)ReadInt16();
        return Members;
    }
    public PacketBase packetPrepare()
    {
        GetPacketMember();
        var packet = GetPacket(Members);
        if (packet == null)
        {
            return null;
        }
        packet._binaryReader = _binaryReader;
        return packet;
    }
    public T PacketReader<T>() where T : PacketBase
    {
        try
        {
            if (this is not T packet)
            {
                return null;
            }
            packet.Read();
            return packet;
        }
        catch (Exception e)
        {
            log.Network.Error($"MsgBase - PacketReader() {e}");
            return null;
        }
    }
    public byte[] PacketBuilder(byte[] suffix)
    {
        try
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort)0);
            writer.Write((ushort)Members);
            this._binaryWriter = writer;
            Build();
            stream.Position = 0;
            writer.Write((ushort)stream.Length);
            stream.Position = stream.Length;
            if (suffix != null)
                writer.Write(suffix);
            return stream.ToArray();
        }
        catch (Exception)
        {
            throw new InvalidOperationException("MsgBase - PacketBuilder()");
        }
    }
    #endregion

    #region IDisposable Implementation

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _binaryReader?.Dispose();
                _binaryWriter?.Dispose();
            }

            // Dispose unmanaged resources
            // (none in this class)

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PacketBase()
    {
        Dispose(false);
    }

    #endregion
}


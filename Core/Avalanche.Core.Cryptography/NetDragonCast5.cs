namespace Avalanche.Core.Cryptography;

public class NetDragonCast5 : INetDragonCipher
{
    private byte[] EncIvec;
    private byte[] DecIvec;
    private int[] EncNum;
    private int[] DecNum;
    private TQCast5Impl Impl;

    public NetDragonCast5()
    {
        Impl = new TQCast5Impl();
        Reset();

    }
    public void GenerateKey(byte[] kb)
    {
        var key = new byte[16];
        for (int i = 0; i < 16; i++)
            key[i] = kb[i];

        Impl.SetKey(key);
    }
    public void GenerateKeys(object[] k)
    {
        var key = k[0] as byte[];
        GenerateKey(key);
    }
    public void Reset()
    {
        EncIvec = new byte[16];
        DecIvec = new byte[16];
        DecNum = new int[8];
        EncNum = new int[8];
    }
    public void Encrypt(Span<byte> src, Span<byte> dst)
    {
        byte c;
        var length = src.Length;
        for (int l = length, n = EncNum[0], inc = 0, outc = 0; l > 0; l--)
        {
            if (n == 0)
            {
                Impl.EncryptBlock(EncIvec, 0, EncIvec, 0);
            }
            c = (byte)((src[inc++] ^ EncIvec[n]) & 0xff);
            dst[outc++] = c;
            EncIvec[n] = c;
            n = n + 1 & 0x07;
            EncNum[0] = n;
        }

    }
    public void Decrypt(Span<byte> src, Span<byte> dst)
    {
        byte c, cc;
        var length = src.Length;
        for (int l = length, n = DecNum[0], inc = 0, outc = 0; l > 0; l--)
        {
            if (n == 0)
            {
                Impl.EncryptBlock(DecIvec, 0, DecIvec, 0);
            }
            cc = src[inc++];
            c = DecIvec[n];
            DecIvec[n] = cc;
            dst[outc] = (byte)((c ^ cc) & 0xff);
            outc++;
            n = n + 1 & 0x07;
            DecNum[0] = n;
        }
    }
}

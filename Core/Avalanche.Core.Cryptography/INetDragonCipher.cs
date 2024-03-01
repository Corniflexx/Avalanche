namespace Avalanche.Core.Cryptography;

public interface INetDragonCipher
{
    void GenerateKeys(object[] seeds);
    void Decrypt(Span<byte> source, Span<byte> destination);
    void Encrypt(Span<byte> source, Span<byte> destination);
}

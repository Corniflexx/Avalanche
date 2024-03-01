namespace Avalanche.Core.Cryptography;

public sealed class DiffieHellman
{
    #region Constants and static properties

    public readonly static PrimeGeneratorService ProbablePrimes;
    private const string DefaultGenerator = "05";
    private const string DefaultPrimativeRoot =
        "E7A69EBDF105F2A6BBDEAD7E798F76A209AD73FB466431E2E7352ED262F8C558" +
        "F10BEFEA977DE9E21DCEE9B04D245F300ECCBBA03E72630556D011023F9E857F";

    #endregion

    #region Key exchange Properties

    public BigInteger PrimeRoot { get; set; }
    public BigInteger Generator { get; set; }
    public BigInteger Modulus { get; set; }
    public BigInteger PublicKey { get; private set; }
    public BigInteger PrivateKey { get; private set; }

    #endregion

    #region Blowfish IV exchange properties

    public byte[] DecryptionIV { get; private set; }
    public byte[] EncryptionIV { get; private set; }

    #endregion

    #region Constructors

    static DiffieHellman()
    {
        ProbablePrimes = new PrimeGeneratorService();
    }

    public DiffieHellman(string p = DefaultPrimativeRoot, string g = DefaultGenerator)
    {
        this.PrimeRoot = new BigInteger(p, 16);
        this.Generator = new BigInteger(g, 16);
        this.DecryptionIV = new byte[8];
        this.EncryptionIV = new byte[8];
    }

    #endregion

    #region Operation Methods

    public async Task ComputePublicKeyAsync()
    {
        this.Modulus ??= await ProbablePrimes.NextAsync();
        this.PublicKey = this.Generator.ModPow(this.Modulus, this.PrimeRoot);
    }

    public void ComputePrivateKey(string clientKeyString)
    {
        var clientKey = new BigInteger(clientKeyString, 16);
        this.PrivateKey = clientKey.ModPow(this.Modulus, this.PrimeRoot);
    }

    public byte[] DecryptPrivateKey()
    {
        var pKey = PrivateKey.ToByteArrayUnsigned();
        var md5 = new MD5Digest();
        var firstRun = new byte[md5.GetDigestSize() * 2];
        md5.BlockUpdate(pKey, 0, pKey.TakeWhile(x => x != 0).Count());
        md5.DoFinal(firstRun, 0);
        Array.Copy(firstRun, 0, firstRun, md5.GetDigestSize(), md5.GetDigestSize());
        var n = Hex.Encode(firstRun);
        md5.BlockUpdate(n, 0, n.Length);
        md5.DoFinal(firstRun, md5.GetDigestSize());
        byte[] key = Hex.Encode(firstRun);

        return key;
    }

    #endregion
}

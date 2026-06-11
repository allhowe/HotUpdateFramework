using System;
using System.IO;
using System.Text;
using HotUpdateFramework;
using UnityEngine;
using YooAsset;

public sealed class HotUpdateOffsetCrypto : IEncryptionServices, IDecryptionServices
{
    private const int Offset = 32;
    private static readonly HotUpdateOffsetCrypto Shared = new HotUpdateOffsetCrypto();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditor()
    {
        Register();
    }
#endif

    public static void Register()
    {
        HotUpdateCryptoProvider.SetServices(Shared, Shared);
    }

    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
        byte[] encryptedData = AddOffset(fileData);
        return new EncryptResult
        {
            Encrypted = true,
            EncryptedData = encryptedData
        };
    }

    public DecryptResult LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        return new DecryptResult
        {
            Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, Offset)
        };
    }

    public DecryptResult LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        return new DecryptResult
        {
            CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, Offset)
        };
    }

    public DecryptResult LoadAssetBundleFallback(DecryptFileInfo fileInfo)
    {
        byte[] fileData = ReadFileData(fileInfo);
        return new DecryptResult
        {
            Result = AssetBundle.LoadFromMemory(fileData, fileInfo.FileLoadCRC)
        };
    }

    public byte[] ReadFileData(DecryptFileInfo fileInfo)
    {
        byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
        return RemoveOffset(fileData);
    }

    public string ReadFileText(DecryptFileInfo fileInfo)
    {
        return Encoding.UTF8.GetString(ReadFileData(fileInfo));
    }

    private static byte[] AddOffset(byte[] fileData)
    {
        byte[] encryptedData = new byte[fileData.Length + Offset];
        Buffer.BlockCopy(fileData, 0, encryptedData, Offset, fileData.Length);
        return encryptedData;
    }

    private static byte[] RemoveOffset(byte[] fileData)
    {
        if (fileData == null || fileData.Length <= Offset)
            return Array.Empty<byte>();

        byte[] decryptedData = new byte[fileData.Length - Offset];
        Buffer.BlockCopy(fileData, Offset, decryptedData, 0, decryptedData.Length);
        return decryptedData;
    }
}

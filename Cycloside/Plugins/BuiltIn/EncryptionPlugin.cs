using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Cycloside.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public class EncryptionPlugin : IPlugin
{
    private EncryptionWindow? _window;
    private TextBox? _inputBox;
    private TextBox? _keyBox;
    private TextBox? _outputBox;
    private ComboBox? _algorithmBox;
    private Action<object?>? _encryptFileHandler;
    private Action<object?>? _decryptFileHandler;

    public string Name => "Encryption";
    public string Description => "AES/RSA text and file encryption";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new EncryptionWindow();
        _inputBox = _window.FindControl<TextBox>("InputBox");
        _keyBox = _window.FindControl<TextBox>("KeyBox");
        _outputBox = _window.FindControl<TextBox>("OutputBox");
        _algorithmBox = _window.FindControl<ComboBox>("AlgorithmBox");
        var encBtn = _window.FindControl<Button>("EncryptButton");
        var decBtn = _window.FindControl<Button>("DecryptButton");
        if (encBtn != null)
            encBtn.Click += async (_, _) => await EncryptTextAsync();
        if (decBtn != null)
            decBtn.Click += async (_, _) => await DecryptTextAsync();

        _encryptFileHandler = async o =>
        {
            if (o is string path && File.Exists(path))
                await EncryptFileAsync(path);
        };
        _decryptFileHandler = async o =>
        {
            if (o is string path && File.Exists(path))
                await DecryptFileAsync(path);
        };
        PluginBus.Subscribe("encryption:encryptFile", _encryptFileHandler);
        PluginBus.Subscribe("encryption:decryptFile", _decryptFileHandler);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(EncryptionPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        if (_encryptFileHandler != null)
        {
            PluginBus.Unsubscribe("encryption:encryptFile", _encryptFileHandler);
            _encryptFileHandler = null;
        }
        if (_decryptFileHandler != null)
        {
            PluginBus.Unsubscribe("encryption:decryptFile", _decryptFileHandler);
            _decryptFileHandler = null;
        }
    }

    private string SelectedAlgorithm() =>
        (_algorithmBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "AES";

    private async Task EncryptTextAsync()
    {
        if (_inputBox == null || _keyBox == null || _outputBox == null) return;
        try
        {
            var text = _inputBox.Text ?? string.Empty;
            var key = _keyBox.Text ?? string.Empty;
            var data = Encoding.UTF8.GetBytes(text);
            var result = await Task.Run(() => EncryptBytes(data, key, SelectedAlgorithm()));
            _outputBox.Text = Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _outputBox.Text = $"Error: {ex.Message}";
        }
    }

    private async Task DecryptTextAsync()
    {
        if (_inputBox == null || _keyBox == null || _outputBox == null) return;
        try
        {
            var text = _inputBox.Text ?? string.Empty;
            var key = _keyBox.Text ?? string.Empty;
            var data = Convert.FromBase64String(text);
            var result = await Task.Run(() => DecryptBytes(data, key, SelectedAlgorithm()));
            _outputBox.Text = Encoding.UTF8.GetString(result);
        }
        catch (Exception ex)
        {
            _outputBox.Text = $"Error: {ex.Message}";
        }
    }

    private async Task EncryptFileAsync(string path)
    {
        if (_window == null || _keyBox == null) return;
        var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Encrypted File",
            SuggestedFileName = Path.GetFileName(path) + ".enc",
            SuggestedStartLocation = start
        });
        if (file?.TryGetLocalPath() is not { } dest) return;
        try
        {
            var key = _keyBox.Text ?? string.Empty;
            var data = await File.ReadAllBytesAsync(path);
            var result = EncryptBytes(data, key, SelectedAlgorithm());
            await File.WriteAllBytesAsync(dest, result);
        }
        catch (Exception ex)
        {
            Logger.Log($"Encrypt file error: {ex.Message}");
        }
    }

    private async Task DecryptFileAsync(string path)
    {
        if (_window == null || _keyBox == null) return;
        var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Decrypted File",
            SuggestedFileName = Path.GetFileNameWithoutExtension(path),
            SuggestedStartLocation = start
        });
        if (file?.TryGetLocalPath() is not { } dest) return;
        try
        {
            var key = _keyBox.Text ?? string.Empty;
            var data = await File.ReadAllBytesAsync(path);
            var result = DecryptBytes(data, key, SelectedAlgorithm());
            await File.WriteAllBytesAsync(dest, result);
        }
        catch (Exception ex)
        {
            Logger.Log($"Decrypt file error: {ex.Message}");
        }
    }

    private static byte[] EncryptBytes(byte[] data, string key, string algorithm)
    {
        return algorithm == "RSA" ? RsaEncrypt(data, key) : AesEncrypt(data, key);
    }

    private static byte[] DecryptBytes(byte[] data, string key, string algorithm)
    {
        return algorithm == "RSA" ? RsaDecrypt(data, key) : AesDecrypt(data, key);
    }

    private static byte[] AesEncrypt(byte[] data, string key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.GenerateIV();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    private static byte[] AesDecrypt(byte[] data, string key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var ivLength = aes.BlockSize / 8;
        var iv = data.AsSpan(0, ivLength).ToArray();
        aes.IV = iv;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(new MemoryStream(data, ivLength, data.Length - ivLength), aes.CreateDecryptor(), CryptoStreamMode.Read);
        cs.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] RsaEncrypt(byte[] data, string key)
    {
        using var rsa = RSA.Create();
        ImportKey(rsa, key);
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    private static byte[] RsaDecrypt(byte[] data, string key)
    {
        using var rsa = RSA.Create();
        ImportKey(rsa, key);
        return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    private static void ImportKey(RSA rsa, string key)
    {
        try
        {
            rsa.ImportFromPem(key.ToCharArray());
        }
        catch
        {
            try
            {
                rsa.FromXmlString(key);
            }
            catch
            {
                // unsupported key format
                throw new InvalidOperationException("Invalid RSA key format");
            }
        }
    }
}

# CYC-2026-030: No Integrity Validation in Plugin Marketplace

**Date:** March 13, 2026  
**Discovered during:** CYC-2026-029 hash collision audit  
**Severity:** HIGH  
**Component:** PluginRepository.cs  
**Status:** 🔥 **CONFIRMED - NO VALIDATION EXISTS**

---

## The Finding

**Searching for hash usage in Cycloside revealed:**

### 1. MD5/SHA-1 Usage (FORENSICS ONLY - Not a security issue)

```csharp
// DigitalForensics.cs - Used for file analysis, NOT security validation
result.Md5Hash = await CalculateFileHashAsync(filePath, "MD5");
result.Sha1Hash = await CalculateFileHashAsync(filePath, "SHA1");

// HackerTerminalPlugin.cs - Used for terminal display, NOT validation
var md5 = System.Security.Cryptography.MD5.Create();
AppendOutput($"🔑 MD5 Hash: {base64}");
```

**Assessment:** These are display/analysis features, not security checks. **LOW risk.**

### 2. Checksum Field Exists But Is NEVER VALIDATED (CRITICAL)

```csharp
// PluginRepository.cs line 396
public class PluginFile
{
    public string Path { get; set; } = "";
    public string Checksum { get; set; } = "";  ← Field exists
    public long Size { get; set; }
}
```

**But in DownloadPluginFilesAsync (line 208-243):**

```csharp
// Download file
var response = await _httpClient.GetAsync(fileUrl);
var content = await response.Content.ReadAsByteArrayAsync();

// NO CHECKSUM VERIFICATION!
await File.WriteAllBytesAsync(filePath, content);
```

**NO VALIDATION OCCURS.**

---

## The Vulnerability

**Currently, plugin downloads have ZERO integrity protection:**

1. User clicks "Install Plugin"
2. Cycloside downloads files from GitHub/CDN
3. **No checksum validation**
4. **No signature verification**
5. Files are written directly to disk
6. Plugin loads and executes

**Attack vectors:**

### Attack 1: Man-in-the-Middle

```
User → [Downloads plugin] → Attacker proxy → Serves malicious plugin
                              ↑
                              No validation - accepts any content
```

### Attack 2: Compromised CDN

```
Legitimate manifest.json:
{
    "name": "CoolTheme",
    "files": [
        {
            "path": "theme.axaml",
            "checksum": "abc123...",  ← Never checked!
            "size": 1024
        }
    ]
}

Attacker compromises CDN:
- Replaces theme.axaml with malicious version
- Checksum in manifest says "abc123..."
- Downloaded file has different hash
- BUT CYCLOSIDE NEVER VERIFIES
- Malicious file executes
```

### Attack 3: DNS Hijacking

```
_repositoryUrl = "https://api.github.com/repos/cycloside/plugins/..."

Attacker hijacks DNS:
- api.github.com → attacker's server
- Serves malicious plugins
- No certificate pinning
- No signature verification
- User unknowingly installs malware
```

---

## Impact

**HIGH severity because:**

1. **Auto-execution** - Plugins run immediately after install
2. **Full system access** - Plugins have .NET capabilities
3. **No user warning** - No indication of untrusted content
4. **Marketplace trust** - Users trust "official" marketplace
5. **Network-based** - Attack doesn't require local access

**This undermines the entire security model.**

---

## Why This Exists

Looking at the code comments:

```csharp
// Line 332: "For now, just reinstall (in production, would check versions)"
public static async Task<bool> UpdatePluginAsync(PluginManifest plugin)
{
    // For now, just reinstall (in production, would check versions)
    return await InstallPluginAsync(plugin);
}
```

**"For now" and "in production, would check" = Pre-release state confirmed.**

**This validates the security audit approach:** Find and fix BEFORE public release.

---

## Comparison to Other Marketplaces

| Marketplace | Integrity Check | Signature | Trust Model |
|-------------|----------------|-----------|-------------|
| **npm** | SHA-512 | No | Registry-validated |
| **PyPI** | MD5 (weak!) | PGP (optional) | Author-validated |
| **Chrome Web Store** | SHA-256 | Code-signed | Google-validated |
| **F-Droid** | SHA-256 | APK signed | Reproducible builds |
| **Cycloside (current)** | ❌ **NONE** | ❌ **NONE** | ⚠️ **HTTPS only** |

**Cycloside is less secure than even PyPI (which uses weak MD5).**

---

## Recommended Fix

### Phase 1: Immediate (Checksum Validation)

```csharp
private static async Task<bool> DownloadPluginFilesAsync(PluginManifest plugin, string pluginPath)
{
    try
    {
        foreach (var file in plugin.Files)
        {
            var fileUrl = $"{_repositoryUrl}/{plugin.Name}/{file.Path}";
            var response = await _httpClient.GetAsync(fileUrl);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"⚠️ Failed to download {file.Path}");
                return false;
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            
            // VALIDATE CHECKSUM
            if (!string.IsNullOrEmpty(file.Checksum))
            {
                var actualChecksum = ComputeSHA256(content);
                if (actualChecksum != file.Checksum)
                {
                    Logger.Log($"❌ Checksum mismatch for {file.Path}");
                    Logger.Log($"   Expected: {file.Checksum}");
                    Logger.Log($"   Actual:   {actualChecksum}");
                    return false;
                }
                Logger.Log($"✅ Checksum verified: {file.Path}");
            }
            else
            {
                Logger.Log($"⚠️ No checksum provided for {file.Path} - integrity cannot be verified");
                // Option: Reject files without checksums in production
            }
            
            var filePath = Path.Combine(pluginPath, file.Path);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, content);
        }

        return true;
    }
    catch (Exception ex)
    {
        Logger.Log($"❌ File download error: {ex.Message}");
        return false;
    }
}

private static string ComputeSHA256(byte[] data)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hash = sha256.ComputeHash(data);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

**Benefits:**
- Detects tampering
- Detects corruption
- Detects MITM attacks
- Simple to implement

**Limitations:**
- Doesn't prove authenticity (manifest could be forged too)
- Still vulnerable if attacker controls manifest

---

### Phase 2: Near-term (GPG Signatures)

```csharp
public class PluginManifest
{
    // ... existing fields ...
    
    // GPG signature of entire manifest
    public string? Signature { get; set; }
    public string? SignerPublicKey { get; set; }
    public string? SignerName { get; set; }
}

private static bool VerifyManifestSignature(PluginManifest manifest)
{
    if (string.IsNullOrEmpty(manifest.Signature) || 
        string.IsNullOrEmpty(manifest.SignerPublicKey))
    {
        Logger.Log("⚠️ Plugin is not signed");
        return false;
    }
    
    // Serialize manifest without signature field
    var manifestCopy = manifest with { Signature = null };
    var manifestJson = JsonSerializer.Serialize(manifestCopy);
    var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
    
    // Verify signature
    using var rsa = RSA.Create();
    rsa.ImportFromPem(manifest.SignerPublicKey);
    
    var signature = Convert.FromBase64String(manifest.Signature);
    var verified = rsa.VerifyData(manifestBytes, signature, 
        HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    
    if (verified)
    {
        Logger.Log($"✅ Signature verified: {manifest.SignerName}");
    }
    else
    {
        Logger.Log($"❌ Invalid signature for plugin: {manifest.Name}");
    }
    
    return verified;
}
```

**Benefits:**
- Proves authenticity (manifest came from claimed author)
- Can't forge without private key
- Protects against compromised CDN

---

### Phase 3: Long-term (Web of Trust)

```csharp
public class PluginTrustInfo
{
    public string AuthorPublicKey { get; set; }
    public List<Endorsement> CommunityEndorsements { get; set; }
    public int TrustScore { get; set; }
    public DateTime FirstSeen { get; set; }
}

public class Endorsement
{
    public string ReviewerName { get; set; }
    public string ReviewerPublicKey { get; set; }
    public string ReviewerSignature { get; set; }
    public DateTime ReviewDate { get; set; }
    public string ReviewComment { get; set; }
}
```

**Benefits:**
- Community-driven trust
- Multiple independent verifications
- Reputation system
- Transparent review process

---

## Severity Justification

**Why HIGH (not CRITICAL):**

1. **Requires network MITM** (not trivial)
2. **HTTPS provides some protection** (attacker needs valid cert or MITM HTTPS)
3. **No evidence of active exploitation** (pre-release)

**But still HIGH because:**

1. **Zero integrity checks** (complete absence of validation)
2. **Auto-execution** (immediate impact)
3. **User trust** (marketplace implies safety)
4. **Easy to fix** (should be fixed before release)

---

## Immediate Actions

1. **Add SHA-256 checksum validation** (Phase 1 - this week)
2. **Require checksums in all manifests** (Phase 1 - this week)
3. **Add signature support** (Phase 2 - before public beta)
4. **Document trust model** (Phase 2 - before public beta)
5. **Implement web of trust** (Phase 3 - v1.0)

---

## Testing

**Create test plugin with wrong checksum:**

```json
{
    "name": "TestPlugin",
    "files": [
        {
            "path": "test.dll",
            "checksum": "0000000000000000000000000000000000000000000000000000000000000000",
            "size": 1024
        }
    ]
}
```

**Expected:** Download fails with "Checksum mismatch"  
**Current:** Download succeeds, no validation

---

## Related Vulnerabilities

- **CYC-2026-029:** Hash collision attacks (if weak algorithms used)
- **CYC-2026-030:** No integrity validation (this vulnerability)
- **CYC-2026-001:** Path traversal (could combine with malicious plugin)
- **CYC-2026-003:** AXAML code execution (payload delivery method)

**Combined attack:**
1. MITM plugin download (CYC-2026-030 - no validation)
2. Deliver plugin with malicious AXAML (CYC-2026-003 - code execution)
3. **Full system compromise**

---

## Recommendation

**BEFORE PUBLIC RELEASE:**
1. Implement Phase 1 (checksum validation)
2. Implement Phase 2 (signatures)
3. Document trust model
4. Security audit of marketplace architecture

**This is a blocking issue for public marketplace launch.**

---

*Status: 🔥 **CONFIRMED - Add to vulnerability catalog immediately***

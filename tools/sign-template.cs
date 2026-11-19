#:property PublishAot=false
// Signs MultibootOS template folders (PLAN.md 25.2). Never part of the builder; run it with the .NET 10 SDK:
//   dotnet run sign-template.cs -- keygen <private-key.pem>                   new key pair; prints the public half for the builder's TrustedKey
//   dotnet run sign-template.cs -- sign <private-key.pem> <key id> <folder>...  writes each folder's template.sig
// The team's private key lives only in the templates repository's CI (T3). Keep every private key out of every repository.
// Self-contained on purpose, so the templates repository can run it without the builder. Manifest() must stay the same as
// MultibootOS.Core.TemplateSignature.Manifest; the builder's test SignTemplateTool_SignsWhatTheBuilderChecks proves they agree.
using System.Security.Cryptography;
using System.Text;

if (args is ["keygen", var newKey])
{
    if (File.Exists(newKey)) { Console.Error.WriteLine($"{newKey} exists; not overwritten."); return 1; }
    using var created = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(newKey))!);
    File.WriteAllText(newKey, created.ExportPkcs8PrivateKeyPem());
    Console.WriteLine(Convert.ToBase64String(created.ExportSubjectPublicKeyInfo()));
    return 0;
}
if (args is not ["sign", var keyFile, var keyId, .. var dirs] || dirs.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(keyId, "^[A-Za-z0-9._-]+$"))
{
    Console.Error.WriteLine("usage: keygen <private-key.pem> | sign <private-key.pem> <key id: letters, digits, . _ -> <template folder>...");
    return 2;
}
using var key = ECDsa.Create();
key.ImportFromPem(File.ReadAllText(keyFile));
foreach (var dir in dirs)
{
    if (!File.Exists(Path.Combine(dir, "template.json"))) { Console.Error.WriteLine($"{dir} has no template.json"); return 1; }
    var signature = Convert.ToBase64String(key.SignData(Manifest(dir), HashAlgorithmName.SHA256));
    File.WriteAllText(Path.Combine(dir, "template.sig"), $"{{\n  \"key\": \"{keyId}\",\n  \"signature\": \"{signature}\"\n}}\n");
    Console.WriteLine($"signed {dir}");
}
return 0;

// "<sha256> <path>\n" for every file but template.sig, sorted by path ('/' separated); .json hashed with LF line ends.
static byte[] Manifest(string dir) => Encoding.UTF8.GetBytes(string.Concat(
    Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
        .Select(f => (Path: Path.GetRelativePath(dir, f).Replace('\\', '/'), File: f))
        .Where(f => f.Path != "template.sig")
        .OrderBy(f => f.Path, StringComparer.Ordinal)
        .Select(f => $"{Convert.ToHexStringLower(SHA256.HashData(f.File.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? Encoding.UTF8.GetBytes(File.ReadAllText(f.File).Replace("\r\n", "\n"))
            : File.ReadAllBytes(f.File)))} {f.Path}\n")));

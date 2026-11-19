#:property PublishAot=false
// Signs MultibootOS template files (PLAN.md 25.2). Never part of the builder; run it with the .NET 10 SDK:
//   dotnet run sign-template.cs -- keygen <private-key.pem>                 new key pair; prints the public half for the builder's TrustedKey
//   dotnet run sign-template.cs -- sign <private-key.pem> <key id> <file>...  signs each template file, replacing its old signature
// The signature is the file's last line, "// signature: <key id> <base64>", over every byte above it with LF line ends.
// The team's private key lives only in the templates repository's CI (T3). Keep every private key out of every repository.
// Self-contained on purpose, so the templates repository can run it without the builder. Split() must stay the same as
// MultibootOS.Core.TemplateSignature.Split; the builder's test SignTemplateTool_SignsWhatTheBuilderChecks proves they agree.
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

if (args is ["keygen", var newKey])
{
    if (File.Exists(newKey)) { Console.Error.WriteLine($"{newKey} exists; not overwritten."); return 1; }
    using var created = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(newKey))!);
    File.WriteAllText(newKey, created.ExportPkcs8PrivateKeyPem());
    Console.WriteLine(Convert.ToBase64String(created.ExportSubjectPublicKeyInfo()));
    return 0;
}
if (args is not ["sign", var keyFile, var keyId, .. var files] || files.Length == 0 || !Regex.IsMatch(keyId, "^[A-Za-z0-9._-]+$"))
{
    Console.Error.WriteLine("usage: keygen <private-key.pem> | sign <private-key.pem> <key id: letters, digits, . _ -> <template file>...");
    return 2;
}
using var key = ECDsa.Create();
key.ImportFromPem(File.ReadAllText(keyFile));
foreach (var file in files)
{
    if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || !File.Exists(file)) { Console.Error.WriteLine($"{file} is not a template file"); return 1; }
    var body = Split(File.ReadAllText(file));
    var signature = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(body), HashAlgorithmName.SHA256));
    File.WriteAllText(file, body + $"// signature: {keyId} {signature}\n", new UTF8Encoding(false));
    Console.WriteLine($"signed {file}");
}
return 0;

// The text with LF line ends and without its signature line, ending in a line end: what is signed.
static string Split(string text)
{
    text = text.Replace("\r\n", "\n");
    var m = Regex.Match(text, @"\n// signature: (\S+) (\S+)\n?$");
    return m.Success ? text[..(m.Index + 1)] : text.EndsWith('\n') ? text : text + "\n";
}

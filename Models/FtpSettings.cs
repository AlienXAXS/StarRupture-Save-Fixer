namespace StarRuptureSaveFixer.Models;

public class FtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 21;
    public string Username { get; set; } = "";
    public string EncryptedPassword { get; set; } = "";
    public bool UseFtps { get; set; } = false;
    public bool PassiveMode { get; set; } = true;
}

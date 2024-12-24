using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Seer;

internal abstract class User
{
    // Constants
    private const string CONFIG_FILE = "./.config.json";
    
    // Attributes
    private string nginxConfig { get; set; } = null!;
    private string nginxLog { get; set; } = null!;
    private string ruleConfiglocation { get; set; } = null!;

    // Abstract methods
    public abstract void Welcome();

    // Shared methods
    protected static string GetConfigName()
    {
        return CONFIG_FILE;
    }

    public char PromptInput()
    {
        Console.Write("Options [b]lock ip, [u]nblock ip, [s]how logs, \n[d]eny list, [i]mplement rules, [r]eset config: ");
        string option = Console.ReadLine() ?? throw new InvalidOperationException();
        return Char.ToLower(option[0]);
    }

    public void LogicHandler(char input)
    {
        switch (input)
        {
            case 'b':
                Console.Write("Enter the IPv4 address: ");
                BlockIp(Console.ReadLine() ?? throw new InvalidOperationException());
                break;
            case 'u':
                Console.Write("Enter the IPv4 address: ");
                UnblockIp(Console.ReadLine() ?? throw new InvalidOperationException());
                break;
            case 's':
                ShowLogs();
                break;
            case 'd':
                DenyList();
                break;
            case 'i':
                BlockIpBasedOnRules();
                break;
            case 'r':
                ResetConfig();
                break;
            default:
                Console.WriteLine("Error: Option invalid!");
                break;
        }
    }
    protected void LoadConfig()
    {
        string jsonMap = File.ReadAllText(GetConfigName());
        Dictionary<string, string> configMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMap) ?? throw new InvalidOperationException();

        Debug.Assert(configMap != null, nameof(configMap) + " != null");
        foreach (var kvp in configMap)
        {
            if (kvp.Key == "nginx_log_location")
            {
                nginxLog = kvp.Value;
            } 
            else if (kvp.Key == "nginx_config_location")
            {
                nginxConfig = kvp.Value;
            }
            else if (kvp.Key == "rule_config_location")
            {
                ruleConfiglocation = kvp.Value;
            }
        }
    }

    public void DenyList()
    {
        var nginxConfigContent = File.ReadLines(nginxConfig).ToList();
        bool isSearchEnabled = false;
        
        Console.WriteLine("Deny List:");
        
        for (int i = 0; i < nginxConfigContent.Count(); i++)
        {
            if (Regex.IsMatch(nginxConfigContent[i], @"^server *{$"))
            {
                isSearchEnabled = true;
            }
            
            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^\tdeny +"))
            {
                Console.WriteLine($"{nginxConfigContent[i]}");
            }
            
            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^}$"))
            {
                break;
            }
        }
    }
    public void ResetConfig()
    {
        File.Delete(GetConfigName());
        Console.WriteLine("Status: Previous configuration was deleted successfully!");
    }
    public void ShowLogs()
    {
        Console.WriteLine(File.ReadAllText(nginxLog));
    }
    public void BlockIp(string ip)
    {
        var nginxConfigContent = File.ReadLines(nginxConfig).ToList();
        string newNginxConfig = "";
        bool isSearchEnabled = false;
        
        for (int i = 0; i < nginxConfigContent.Count(); i++)
        {
            if (Regex.IsMatch(nginxConfigContent[i], @"^server *{$"))
            {
                isSearchEnabled = true;
            }
            
            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^\tdeny +" + ip + @";$"))
            {
                continue;
            }
            
            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^}$"))
            {
                newNginxConfig += $"\tdeny {ip};\n";
                isSearchEnabled = false;
                Console.WriteLine($"Status: {ip} added to deny list!");
            }
            
            newNginxConfig += $"{nginxConfigContent[i]}\n";
        }
        
        File.WriteAllText(nginxConfig, newNginxConfig);
        
        // Execute reloading for nginx to apply changes
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "sudo";
            process.StartInfo.Arguments = "nginx -s reload";
            process.StartInfo.UseShellExecute = false; // Required to redirect output
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            // Print results
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine("nginx: " + output);
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("nginx: " + error);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    public void UnblockIp(string ip)
    {
        var nginxConfigContent = File.ReadLines(nginxConfig).ToList();
        string newNginxConfig = "";
        bool isSearchEnabled = false;
        
        for (int i = 0; i < nginxConfigContent.Count(); i++)
        {
            if (Regex.IsMatch(nginxConfigContent[i], @"^server *{$"))
            {
                isSearchEnabled = true;
            }
            
            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^}$"))
            {
                isSearchEnabled = false;
            }

            if (isSearchEnabled && Regex.IsMatch(nginxConfigContent[i], @"^\tdeny +" + ip + @";$"))
            {
                Console.WriteLine($"Status: {ip} removed from deny list!");
                continue;
            }
            
            newNginxConfig += $"{nginxConfigContent[i]}\n";
        }
        
        File.WriteAllText(nginxConfig, newNginxConfig);
        
        // Execute reloading for nginx to apply changes
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "sudo";
            process.StartInfo.Arguments = "nginx -s reload";
            process.StartInfo.UseShellExecute = false; // Required to redirect output
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            // Print results
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine("nginx: " + output);
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("nginx: " + error);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void BlockIpBasedOnRules()
    {
        var ruleset = File.ReadLines(ruleConfiglocation);
        var logs = File.ReadLines(nginxLog);
        foreach (var entry in logs)
        {
            foreach (var rule in ruleset)
            {
                if (entry.Contains(rule))
                {
                    var ipMatch = Regex.Matches(entry, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                    
                    if (ipMatch.Count() > 0)
                    {
                        BlockIp(ipMatch[0].Value);
                    }
                    else
                    {
                        Console.WriteLine("Error: No matching IP found.");
                    }
                }
            }
        }
    }
}

internal class NewUser : User
{
    public override void Welcome()
    {
        Console.WriteLine(">>=======================<<\n" +
                          "||  ____                 ||\n" +
                          "|| / ___|  ___  ___ _ __ ||\n" +
                          "|| \\___ \\ / _ \\/ _ \\ '__|||\n" +
                          "||  ___) |  __/  __/ |   ||\n" +
                          "|| |____/ \\___|\\___|_|   ||\n" +
                          "||                       ||\n" +
                          ">>=======================<<");
        Console.WriteLine("Welcome to Seer! Let's set up your configuration file.");
    }

    public void CreateConfig()
    {
        // TODO: Change the following for the GUI
        Console.Write("Enter absolute path of nginx logs: ");
        string nginxLogLocation = Console.ReadLine() ?? throw new InvalidOperationException();
        Console.Write("Enter absolute path of nginx config: ");
        string nginxConfigLocation = Console.ReadLine() ?? throw new InvalidOperationException();
        Console.Write("Enter absolute path of rule config: ");
        string ruleConfiglocation = Console.ReadLine() ?? throw new InvalidOperationException();
        
        Dictionary<string, string> configMap = new Dictionary<string, string>
        {
            { "nginx_log_location", nginxLogLocation },
            { "nginx_config_location", nginxConfigLocation },
            { "rule_config_location", ruleConfiglocation }
        };
        string jsonConfig = JsonSerializer.Serialize(configMap, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetConfigName(), jsonConfig);

        // Load the config
        LoadConfig();
    }
}

internal class ExistingUser : User
{
    public override void Welcome()
    {
        Console.WriteLine(">>=======================<<\n" +
                          "||  ____                 ||\n" +
                          "|| / ___|  ___  ___ _ __ ||\n" +
                          "|| \\___ \\ / _ \\/ _ \\ '__|||\n" +
                          "||  ___) |  __/  __/ |   ||\n" +
                          "|| |____/ \\___|\\___|_|   ||\n" +
                          "||                       ||\n" +
                          ">>=======================<<");
        Console.WriteLine("Welcome to Seer! ");
        
        // Load the config
        LoadConfig();
    }
}
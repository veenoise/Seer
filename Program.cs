// See https://aka.ms/new-console-template for more information

using Seer;

public class Program {
    static void Main()
    {
        const string CONFIG_FILE = "./.config.json";
        while (true)
        {
            try
            {
                // Load the content of the CONFIG_FILE
                FileStream fs = new FileStream(CONFIG_FILE, FileMode.Open);

                ExistingUser user = new ExistingUser();
                user.Welcome();
                user.LogicHandler(user.PromptInput());
            }
            catch (FileNotFoundException)
            {
                // Trigger first-time user guide
                NewUser user = new NewUser();
                user.Welcome(); 
                user.CreateConfig();
                user.LogicHandler(user.PromptInput());
            }
        }
    }
}
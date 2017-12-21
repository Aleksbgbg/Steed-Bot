namespace SteedBot
{
    using System.IO;
    using System.Threading.Tasks;

    using SteedBot.Bot;
    using SteedBot.Commands;
    using SteedBot.Commands.Admin;
    using SteedBot.Token;

    internal static class Program
    {
        private static async Task Main()
        {
            {
                string ReadFile(string filename) => File.ReadAllText(filename).Replace("\r\n", "\n");

                // Token retrieved from static TokenRetriever class to prevent token theft (TokenRetriever will not be available on GitHub)
                Steed bot = new Steed(TokenRetriever.RetrieveToken(), ReadFile(@"Commands\Commands.txt"), ReadFile(@"Commands\Admin\AdminCommands.txt"));

#if !DEBUG
                bot.RegisterCommandClass<CommandActions>();
                bot.RegisterCommandClass<AdminCommandActions>();
#else
                bot.RegisterDebugCommandClass<CommandActions>();
                bot.RegisterDebugCommandClass<AdminCommandActions>();
#endif // !DEBUG

                await bot.StartAsync();
            }

            await Task.Delay(-1);
        }
    }
}
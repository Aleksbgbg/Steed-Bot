namespace Steed.Bot
{
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Commands;
    using DSharpPlus;
    using Token;

    internal static class Program
    {
        private static readonly WebClient WebClient = new WebClient();

        private static void Main() => Task.Run(async () =>
        {
            {
                Command[] commands = Regex.Matches(AcquireResourceFile("Steed.Bot.Commands.Commands.txt"), "(?<Command>!.+)\n(?<Regex>.+)\n(?<Description>.+)").Cast<Match>().Select(match => new Command(match.Groups["Command"].Value, match.Groups["Regex"].Value, match.Groups["Description"].Value)).ToArray();

                // Token retrieved from static TokenRetriever class to prevent token theft (TokenRetriever will not be available on GitHub)
                DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { AutoReconnect = true, Token = TokenRetriever.RetrieveToken() });

                await discordClient.ConnectAsync();

                // Not required at the moment
                //
                // DiscordGuild steedGuild = await discordClient.GetGuildAsync(389407914366074901);
                //
                // Dictionary<string, DiscordMember> members = (await steedGuild.GetAllMembersAsync()).ToDictionary(member => member.Username, member => member);
                // Dictionary<string, DiscordChannel> channels = (await steedGuild.GetChannelsAsync()).ToDictionary(channel => channel.Name, channel => channel);

                discordClient.MessageCreated += async e =>
                {
                    if (!e.Message.Content.StartsWith("!") || e.Message.Author.IsBot) return;

                    var matchedCommand = commands.Select((command, index) => new { Command = command, Index = index }).FirstOrDefault(command => command.Command.IsMatch(e.Message.Content));

                    if (matchedCommand == null)
                    {
                        var closestMatch = commands.Select(command => new { Command = command, Distance = command.ComputeLevensteinDistance(e.Message.Content) }).OrderBy(command => command.Distance).First();

                        await e.Message.RespondAsync(closestMatch.Distance <= 5 ? $"{e.Author.Mention}\nInvalid command.\nClosest match:\n{closestMatch.Command.HelpText}" : $"{e.Author.Mention}\nInvalid command.");

                        return;
                    }

                    switch (matchedCommand.Index)
                    {
                        case 0: // help
                            await e.Message.RespondAsync($"{e.Author.Mention}\n**Commands:**\n{string.Join("\n\n", commands.Select(command => command.HelpText))}");
                            break;

                        case 1: // changelog
                            await e.Message.RespondAsync($"{e.Author.Mention}\n**Change Log:**\n{WebClient.DownloadString("http://steedservers.000webhostapp.com/steedbuild/updatelog.txt")}");
                            break;
                    }
                };
            }

            await Task.Delay(-1);
        }).GetAwaiter().GetResult();

        // Not required at the moment
        // private static string AcquireResourceFile(string fileNamespace, string filename) => AcquireResourceFile(string.Concat(fileNamespace, filename));

        private static string AcquireResourceFile(string filename)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            using (StreamReader streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))) return streamReader.ReadToEnd().Replace("\r", string.Empty);
        }
    }
}
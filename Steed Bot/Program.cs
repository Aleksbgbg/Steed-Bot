namespace Steed.Bot
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Commands;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using Token;

    internal static class Program
    {
        private static readonly WebClient WebClient = new WebClient();

        private static void Main() => Task.Run(async () =>
        {
            // Commands currently stored in text file Commands.txt. If bot is to be ran on a server, this will need to be embeeded into the assembly and ran from there.
            Command[] commands = Regex.Matches(File.ReadAllText(@"E:\Programming\C#\Steed Bot\Steed Bot\Commands\Commands.txt").Replace("\r", string.Empty), "(?<Command>!.+)\n(?<Regex>.+)\n(?<Description>.+)").Cast<Match>().Select(match => new Command(match.Groups["Command"].Value, match.Groups["Regex"].Value, match.Groups["Description"].Value)).ToArray();

            // Token retrieved from static TokenRetriever class to prevent token theft (TokenRetriever will not be available on GitHub)
            DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { AutoReconnect = true, Token = TokenRetriever.RetrieveToken() });

            await discordClient.ConnectAsync();

            DiscordGuild steedGuild = await discordClient.GetGuildAsync(389407914366074901);

            Dictionary<string, DiscordMember> members = (await steedGuild.GetAllMembersAsync()).ToDictionary(member => member.Username, member => member);

            Dictionary<string, DiscordChannel> channels = (await steedGuild.GetChannelsAsync()).ToDictionary(channel => channel.Name, channel => channel);

            discordClient.MessageCreated += async e =>
            {
                if (!e.Message.Content.StartsWith("!")) return;

                Command matchedCommand = commands.FirstOrDefault(command => command.IsMatch(e.Message.Content));

                if (matchedCommand == null)
                {
                    var closestMatch = commands.Select(command => new { Command = command, Distance = command.ComputeLevensteinDistance(e.Message.Content) }).OrderBy(command => command.Distance).First();

                    await e.Message.RespondAsync(closestMatch.Distance <= 5 ? $"{e.Author.Mention}\nInvalid command.\nClosest match:\n{closestMatch.Command.HelpText}" : $"{e.Author.Mention}\nInvalid command.");

                    return;
                }

                switch (matchedCommand.CommandString)
                {
                    case "!changelog":
                        await e.Message.RespondAsync($"{e.Author.Mention}\n**Change Log:**\n{WebClient.DownloadString("http://steedservers.000webhostapp.com/steedbuild/updatelog.txt")}");
                        break;
                }
            };

            await Task.Delay(-1);
        }).GetAwaiter().GetResult();
    }
}
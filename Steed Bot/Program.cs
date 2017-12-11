namespace Steed.Bot
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Commands;
    using DSharpPlus;
    using Token;
    using static System.Console;

    internal static class Program
    {
        private const string BaseSteedServerUrl = "http://steedservers.000webhostapp.com/steedbuild";

        private static readonly WebClient WebClient = new WebClient();

        private static readonly Command[] Commands = Regex.Matches(AcquireResourceFile("Steed.Bot.Commands.Commands.txt"), "(?<Command>.+)\n(?<Syntax>!.+)\n(?<Regex>.+)\n(?<Description>.+)").Cast<Match>().Select(match => new Command(match.Groups["Command"].Value, match.Groups["Syntax"].Value, match.Groups["Regex"].Value, match.Groups["Description"].Value)).ToArray();

        private static void Main() => Task.Run(async () =>
        {
            {
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

                    var matchedCommand = Commands.Select((command, index) => new { Command = command, Index = index, Match = command.Match(e.Message.Content) }).FirstOrDefault(command => command.Match.Success);

                    async Task RespondAsync(string message) => await e.Message.RespondAsync(string.Join("\n", e.Author.Mention, message));

                    if (matchedCommand == null)
                    {
                        var closestMatch = Commands.Select(command => new { Command = command, Distance = command.ComputeLevensteinDistance(e.Message.Content) }).OrderBy(command => command.Distance).First();

                        await RespondAsync(closestMatch.Distance <= 3 ? $"Invalid syntax.\nClosest match:\n{closestMatch.Command.HelpText}" : "Invalid syntax. Type `!help` for a syntax prompt.");

                        return;
                    }

                    switch (matchedCommand.Index)
                    {
                        case 0: // help
                            await RespondAsync($"**Commands:**\n{string.Join("\n\n", Commands.Select(command => command.HelpText))}");
                            break;

                        case 1: // steed
                            for (int index = 1; index < 4; ++index)
                            {
                                switch (matchedCommand.Match.Groups[index].Value)
                                {
                                    case "changelog":
                                        await RespondAsync($"**Change Log:**\n{WebClient.DownloadString($"{BaseSteedServerUrl}/updatelog.txt")}");
                                        break;

                                    case "version":
                                        // Arbitrarily inserting spaces between the DateTime string downloaded from steedservers in order to be parsed correctly - might break in the future
                                        await RespondAsync($"The latest Steed version was released on {DateTime.Parse(WebClient.DownloadString($"{BaseSteedServerUrl}/version.txt").Insert(2, " ").Insert(5, " ")).ToLongDateString()}.");
                                        break;

                                    case "update":
                                        await e.Message.RespondWithFileAsync(WebRequest.Create($"{BaseSteedServerUrl}/steedapp.zip").GetResponse().GetResponseStream(), "steedapp.zip", e.Author.Mention);
                                        break;
                                }
                            }
                            break;

                        default: // Commands mentioned in the 'Commands.txt' file which do not have an associated action
                            await RespondAsync("The command you invoked is not yet supported by Steed Bot.");
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
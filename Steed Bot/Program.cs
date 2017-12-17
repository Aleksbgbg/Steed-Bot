namespace Steed.Bot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;
    using Commands;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Token;

    internal static class Program
    {
        private const string BaseSteedServerUrl = "http://steedservers.000webhostapp.com/steedbuild";

        private static readonly WebClient WebClient = new WebClient();

        private static readonly Random Random = new Random();

        private static readonly Command[] Commands = Regex.Matches(
            AcquireResourceFile("Steed.Bot.Commands.Commands.txt"), "(?<Command>.+)\n(?<Syntax>!.+)\n(?<Regex>.+)\n(?<Description>.+)"
            )
            .Cast<Match>()
            .Select(match => new Command(match.Groups["Command"].Value, match.Groups["Syntax"].Value, match.Groups["Description"].Value, match.Groups["Regex"].Value))
            .ToArray();

        private static async Task Main()
        {
            {
                // Token retrieved from static TokenRetriever class to prevent token theft (TokenRetriever will not be available on GitHub)
                DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { Token = TokenRetriever.RetrieveToken() });

                DiscordGuild steedGuild = await discordClient.GetGuildAsync(389407914366074901);

                ulong[] adminIds = (await steedGuild.GetAllMembersAsync()).Where(member => member.Roles.Max()?.Permissions.HasPermission(Permissions.Administrator) ?? false).Select(member => member.Id).ToArray(); //{ 288017264446537739, 152096276517748736 };

                Dictionary<string, DiscordChannel> channels = (await steedGuild.GetChannelsAsync()).ToDictionary(channel => channel.Name, channel => channel);

                AdminCommand[] adminCommands = { new AdminCommand("^!announce update$", () => channels["news-updates"].SendMessageAsync($"@everyone\n**A new update of Steed is available now! Open Steed and the update will be installed.**\n{DownloadFile("updatelog.txt")}")) };

                discordClient.MessageCreated += async e =>
                {
                    if (!e.Message.Content.StartsWith("!") || e.Message.Author.IsBot) return;

                    if (adminIds.Contains(e.Author.Id))
                    {
                        AdminCommand commandMatch = adminCommands.FirstOrDefault(command => command.IsMatch(e.Message.Content));

                        if (commandMatch != null)
                        {
                            async Task ProcessConfirmAsync(MessageCreateEventArgs args)
                            {
                                if (args.Author != e.Author || args.Message.Content != "confirm") return;

                                discordClient.MessageCreated -= ProcessConfirmAsync;

                                await RespondAsync("You gaat it bro.");
                                commandMatch.Execute();
                            }

                            discordClient.MessageCreated += ProcessConfirmAsync;
                            await RespondAsync("`confirm` please. 30 seconds left.");

                            Timer confirmTimer = new Timer(30_000);

                            void OnElapsed(object sender, ElapsedEventArgs args)
                            {
                                confirmTimer.Elapsed -= OnElapsed;
                                discordClient.MessageCreated -= ProcessConfirmAsync;
                            }

                            confirmTimer.Elapsed += OnElapsed;
                            confirmTimer.Start();

                            return;
                        }
                    }

                    var matchedCommand = Commands.Select((command, index) => new { Command = command, Index = index, Match = command.Match(e.Message.Content) }).FirstOrDefault(command => command.Match.Success);

                    async Task RespondAsync(string message) => await e.Message.RespondAsync(string.Join("\n", e.Author.Mention, message));

                    if (matchedCommand == null)
                    {
                        var closestMatch = Commands.Select(command => new { Command = command, Distance = command.ComputeLevensteinDistance(e.Message.Content.Split().First().TrimStart('!')) }).OrderBy(command => command.Distance).First();

                        await RespondAsync(closestMatch.Distance <= 3 ? $"Invalid syntax.\nClosest match:\n{closestMatch.Command.HelpText}" : "Invalid syntax. Type `!help` for a syntax prompt.");

                        return;
                    }

                    async Task RespondWithFileAsync(string filename) => await e.Message.RespondWithFileAsync(WebRequest.Create($"{BaseSteedServerUrl}/{filename}").GetResponse().GetResponseStream(), filename, e.Author.Mention);

                    switch (matchedCommand.Index)
                    {
                        case 0: // help
                            await RespondAsync($"**Commands:**\n{string.Join("\n\n", Commands.Select(command => command.HelpText))}");
                            break;

                        case 1: // steed
                            string[] matchGroups = matchedCommand.Match.Groups.Cast<Group>().Skip(1).Select(group => group.ToString()).ToArray();

                            if (matchGroups.All(match => match == string.Empty)) // Match is '!steed'
                            {
                                await RespondAsync(Random.Next(2) == 1 ? "Steed Bot is online." : "Yes?");
                                break;
                            }

                            foreach (string match in matchGroups)
                            {
                                switch (match)
                                {
                                    case "changelog":
                                        await RespondAsync($"**Change Log:**\n{DownloadFile("updatelog.txt")}");
                                        break;

                                    case "version":
                                        // Arbitrarily formatting the DateTime string downloaded from steedservers in order to be parsed correctly - most likely will break (again) in the future
                                        await RespondAsync($"The latest Steed version was released on {DateTime.Parse(string.Concat(DownloadFile("version.txt").Insert(2, " ").Insert(5, " ").TakeWhile(character => character != '#'))).ToLongDateString()}.");
                                        break;

                                    case "update":
                                        await RespondWithFileAsync("steedapp.zip");
                                        break;

                                    case "screenshot":
                                        await RespondWithFileAsync("screenshot.png");
                                        break;
                                }
                            }
                            break;

                        default: // Commands mentioned in the 'Commands.txt' file which do not have an associated action
                            await RespondAsync("The command you invoked is not yet supported by Steed Bot.");
                            break;
                    }
                };

                await discordClient.ConnectAsync();
            }

            await Task.Delay(-1);
        }

        private static string AcquireResourceFile(string filename)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            using (StreamReader streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))) return streamReader.ReadToEnd().Replace("\r", string.Empty);
        }

        private static string DownloadFile(string filename) => WebClient.DownloadString($"{BaseSteedServerUrl}/{filename}");
    }
}
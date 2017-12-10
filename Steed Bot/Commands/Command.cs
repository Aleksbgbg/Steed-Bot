namespace Steed.Bot.Commands
{
    using System;
    using System.Text.RegularExpressions;

    internal class Command
    {
        internal string CommandString { get; }

        private readonly string description;

        private readonly Regex regex;

        internal Command(string commandString, string regex, string description)
        {
            CommandString = commandString;
            this.description = description;
            this.regex = new Regex(regex, RegexOptions.Compiled);
        }

        internal string HelpText => $"**{CommandString}:**\n- {description}";

        internal bool IsMatch(string text) => regex.IsMatch(text);

        internal int ComputeLevensteinDistance(string source)
        {
            int[,] distances = new int[source.Length + 1, CommandString.Length + 1];

            if (source.Length == 0) return CommandString.Length;

            if (CommandString.Length == 0) return source.Length;

            for (int column = -1; column < source.Length; distances[++column, 0] = column)
            {
            }

            for (int row = -1; row < CommandString.Length; distances[0, ++row] = row)
            {
            }

            for (int column = 1; column <= source.Length; column++)
                for (int row = 1; row <= CommandString.Length; row++)
                {
                    int previousColumn = column - 1, previousRow = row - 1;

                    distances[column, row] = Math.Min(Math.Min(distances[previousColumn, row] + 1, distances[column, previousRow] + 1), distances[previousColumn, previousRow] + (CommandString[previousRow] == source[previousColumn] ? 0 : 1));
                }

            return distances[source.Length, CommandString.Length];
        }
    }
}
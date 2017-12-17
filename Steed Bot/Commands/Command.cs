namespace Steed.Bot.Commands
{
    using System;
    using System.Text.RegularExpressions;

    internal class Command : BaseCommand
    {
        private readonly string commandString;

        private readonly string syntax;

        private readonly string description;

        internal Command(string commandString, string syntax, string description, string regex) : base(regex)
        {
            this.commandString = commandString;
            this.syntax = syntax;
            this.description = description;
        }

        internal string HelpText => $"`{commandString}`: {description}\nSyntax:```{syntax}```";

        internal Match Match(string command) => regex.Match(command);

        internal int ComputeLevensteinDistance(string source)
        {
            int[,] distances = new int[source.Length + 1, commandString.Length + 1];

            if (source.Length == 0) return commandString.Length;

            if (commandString.Length == 0) return source.Length;

            for (int column = -1; column < source.Length; distances[++column, 0] = column)
            {
            }

            for (int row = -1; row < commandString.Length; distances[0, ++row] = row)
            {
            }

            for (int column = 1; column <= source.Length; ++column)
                for (int row = 1; row <= commandString.Length; ++row)
                {
                    int previousColumn = column - 1;
                    int previousRow = row - 1;

                    distances[column, row] = Math.Min(Math.Min(
                        distances[previousColumn, row] + 1,
                        distances[column, previousRow] + 1),
                        distances[previousColumn, previousRow] + (commandString[previousRow] == source[previousColumn] ? 0 : 1)
                        );
                }

            return distances[source.Length, commandString.Length];
        }
    }
}
using FileSearchCLI.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileSearchCLI.Services.Arguments
{
    /*public class ArgumentProcessor
    {
        private readonly ArgumentHelper _argumentHelper;

        public ArgumentProcessor(ArgumentHelper argumentHelper)
        {
            _argumentHelper = argumentHelper;
        }

        private IOperation? ProcessArgument(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        ref bool indexRequested,
        ref bool searchRequested,
        ref bool removeDatabaseRequested,
        ref string? fileNamePattern,
        List<string> fileFormats)
        {
            var (option, inlineValue) = _argumentHelper.SplitArgument(argument);

            switch (option)
            {
                case "-h":
                case "--help":
                    if (inlineValue is not null)
                        throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                    //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);
                    return new ArgumentParseResult(null, null, true);
                case "-v":
                case "--version":
                    if (inlineValue is not null)
                        throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                    //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);
                    return new ArgumentParseResult(null, null, false, true);
                case "-i":
                case "--index":
                    if (inlineValue is not null)
                        throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                    //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);
                    if (indexRequested)
                        throw new ArgumentException("The index option can only be specified once.");
                    //return new ArgumentParseResult(null, "The index option can only be specified once.", false);
                    if (searchRequested)
                        throw new ArgumentException("Indexing and searching cannot be requested together.");
                    //return new ArgumentParseResult(null, "Indexing and searching cannot be requested together.", false);
                    indexRequested = true;
                    return null;
            }

            if (option is "-i" or "--index")
            {
                if (inlineValue is not null)
                    throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

                if (indexRequested)
                    throw new ArgumentException("The index option can only be specified once.");
                //return new ArgumentParseResult(null, "The index option can only be specified once.", false);

                if (searchRequested)
                    throw new ArgumentException("Indexing and searching cannot be requested together.");
                //return new ArgumentParseResult(null, "Indexing and searching cannot be requested together.", false);

                indexRequested = true;
                return null;
            }

            if (option is "-r" or "--remove-database")
            {
                if (inlineValue is not null)
                    throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

                if (removeDatabaseRequested)
                    throw new ArgumentException("The remove-database option can only be specified once.");
                //return new ArgumentParseResult(null, "The remove-database option can only be specified once.", false);

                removeDatabaseRequested = true;
                return null;
            }

            if (option is "-s" or "--search")
            {
                if (inlineValue is not null)
                    throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

                if (searchRequested)
                    throw new ArgumentException("The search option can only be specified once.");
                //return new ArgumentParseResult(null, "The search option can only be specified once.", false);

                if (indexRequested)
                    throw new ArgumentException("Indexing and searching cannot be requested together.");
                //return new ArgumentParseResult(null, "Indexing and searching cannot be requested together.", false);

                searchRequested = true;
                return null;
            }

            if (option is "-n" or "--name")
                return ProcessNameOption(args, ref index, option, inlineValue, ref fileNamePattern);

            if (option is "-f" or "--format")
                return ProcessFormatOption(args, ref index, option, inlineValue, fileFormats);

            return new ArgumentParseResult(null, $"Unknown argument '{argument}'.", false);
        }
    }*/
}

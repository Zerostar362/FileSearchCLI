using System;
using System.Collections.Generic;
using System.Text;

namespace FileSearchCLI.Services.Arguments
{
    public class ArgumentHelper
    {
        public (string Option, string? InlineValue) SplitArgument(string argument)
        {
            var separatorIndex = argument.IndexOf('=');
            return separatorIndex >= 0
                ? (argument[..separatorIndex], argument[(separatorIndex + 1)..])
                : (argument, null);
        }
    }
}

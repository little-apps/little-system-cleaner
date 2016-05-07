using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Little_System_Cleaner.Misc
{
    public class ParseArgs
    {
        /// <summary>
        /// This contains the parameters, with the key being the parameter name and the value being that parameters value.
        /// </summary>
        /// <remarks>If no parameter is specified (ie: "--name") then the value is "true"</remarks>
        public Dictionary<string, string> Arguments { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Anything specified at the end of the parameters
        /// </summary>
        /// <remarks>This can be things like a file or IP address</remarks>
        public List<string> Items { get; } = new List<string>();

        //
        /// <summary>
        /// Parses arguments and converts them into the relative arrays
        /// </summary>
        /// <remarks>
        /// Valid parameters forms:
        /// {-,/,--}param{ ,=,:}((",')value(",'))
        /// Examples:
        /// -param1 value1 --param2 /param3:"Test-:-work"
        ///   /param4=happy -param5 '--=nice=--'
        /// </remarks>
        /// <param name="args">Parameters specified, split into an array by a space</param>
        public ParseArgs(IEnumerable<string> args)
        {
            // Define -, --, /, : and = as valid delimiters.  Ignore : and = if enclosed in quotes.
            var validDelims = new Regex(@"^-{1,2}|^/|[^['""]?.*]=['""]?$|[^['""]?.*]:['""]?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Define anything enclosed with double quotes as a match.  We'll use this to replace
            // the entire string with only the part that matches (everything but the quotes)
            var quotedString = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string currentParam = null;

            foreach (var parts in args.Select(arg => validDelims.Split(arg, 3)))
            {
                if (string.IsNullOrEmpty(currentParam) && !string.IsNullOrEmpty(parts[0]))
                {
                    Items.Add(quotedString.IsMatch(parts[0]) ? quotedString.Replace(parts[0], "$1") : parts[0]);
                }

                switch (parts.Length)
                {
                    // no special characters present.  we assume this means that this part
                    // represents a value to the previously provided parameter.
                    // For example, if we have: "--MyTestArg myValue"
                    // currentParam would currently be set to "--MyTestArg"
                    // parts[0] would hold "myValue", to be assigned to MyTestArg
                    case 1:
                        if (currentParam != null)
                        {
                            if (!Arguments.ContainsKey(currentParam))
                                Arguments.Add(currentParam, quotedString.Replace(parts[0], "$1"));

                            currentParam = null;
                        }
                        break;

                    // One split ocurred, meaning we found a parameter delimiter
                    // at the start of arg, but nothing to denote a value.
                    // example: --MyParam
                    case 2:
                        // We already had a parameter with no value last time through the loop.
                        // That means we have no explicit value to give currentParam. We'll default it to "true"
                        if (currentParam != null && !Arguments.ContainsKey(currentParam))
                            Arguments.Add(currentParam, "true");

                        // Store our value-less param and grab the next arg to see if it has our value
                        // parts[0] only contains the opening delimiter -, --, or /,
                        // so we go after parts[1] for the actual param name
                        currentParam = parts[1];
                        break;

                    // Two splits occurred.  We found a starting parameter delimiter,
                    // a parameter name, and another delimiter denoting a value for this parameter
                    // Example: --MyParam=MyValue   or   --MyParam:MyValue
                    case 3:
                        // We already had a parameter with no value last time through the loop.
                        // That means we have no explicit value to give currentParam. We'll default it to "true"
                        if (currentParam != null && !Arguments.ContainsKey(currentParam))
                            Arguments.Add(currentParam, "true");

                        // Store the good param name
                        currentParam = parts[1];

                        // Ignores parameters that have already been presented, not thrilled about this approach...
                        if (!Arguments.ContainsKey(currentParam))
                            Arguments.Add(currentParam, quotedString.Replace(parts[2], "$1"));

                        // Reset currentParam, we already have both parameter and value for this arg
                        currentParam = null;
                        break;
                }
            }

            // Final cleanup, we may still have a parameter at the end of the args string that didn't get a value
            if (currentParam == null)
                return;

            if (!Arguments.ContainsKey(currentParam))
                Arguments.Add(currentParam, "true");
        }
    }
}
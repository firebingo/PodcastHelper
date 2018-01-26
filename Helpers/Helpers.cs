using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PodcastHelper.Helpers
{
	public static class HelperMethods
	{
		public static int ParseEpisodeNumber(string toParse)
		{
			var ret = -1;

			var reg = new Regex("#?[0-9]{1,5}");
			var matches = reg.Matches(toParse);
			var hashStart = matches.Cast<Match>().FirstOrDefault(x => x.Value.Contains("#"));
			if(hashStart != null)
			{
				var removehash = hashStart.Value.Replace("#", "");
				if(int.TryParse(removehash, out var p))
				{
					return p;
				}
			}
			if(matches.Count > 0)
			{
				if (int.TryParse(matches[0].Value, out var p))
				{
					return p;
				}
			}

			return ret;
		}
	}

	public static class String_ExtensionMethods
	{
		public static bool ContainsInvariant(this string str, string innerText)
		{
			return str.IndexOf(innerText, StringComparison.InvariantCultureIgnoreCase) != -1;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

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

		public static int GetEpisodeNumberFromFeed(SyndicationItem item)
		{
			var ret = -1;

			if (!string.IsNullOrWhiteSpace(item.Id) && item.Id.Length < 5 && !Guid.TryParse(item.Id, out _))
				ret = ParseEpisodeNumber(item.Id);
			if (ret == -1)
				ret = ParseEpisodeNumber(item.Title.Text);
			if (ret == -1)
			{
				Uri enclosure = null;
				if (item.Links != null)
					enclosure = item.Links.FirstOrDefault(x => x.RelationshipType.ToLowerInvariant() == "enclosure")?.Uri;
				ret = ParseEpisodeNumber(enclosure.Segments.Last());
			}

			return ret;
		}

		public static List<string> ReadKeywords(SyndicationElementExtension ele)
		{
			var ret = new List<string>();

			var reader = ele.GetReader();
			while (reader.Read())
			{
				if (!string.IsNullOrWhiteSpace(reader.Value))
				{
					var value = reader.Value;
					var split = value.Split(',');
					if (split.Length > 0)
					{
						foreach (var s in split)
						{
							ret.Add(s);
						}
					}
					else
					{
						ret.Add(reader.Value);
					}
				}
			}

			return ret;
		}

		public static TimeSpan ReadDuration(SyndicationElementExtension ele)
		{
			var ret = new TimeSpan();

			var reader = ele.GetReader();
			reader.Read();
			if (!string.IsNullOrWhiteSpace(reader.Value))
			{
				if(TimeSpan.TryParse(reader.Value, out ret))
					return ret;
			}

			return ret;
		}

		public static string FindXmlAttribute(XmlNode node, string ToFind)
		{
			var ret = string.Empty;

			var atts = node.Attributes;
			if (atts != null && atts.Count > 0)
			{
				foreach (XmlAttribute att in atts)
				{
					if (att.Name.ToLowerInvariant() == ToFind)
					{
						ret = att.Value;
						break;
					}
				}
			}

			return ret;
		}
	}

	public static class String_ExtensionMethods
	{
		public static bool ContainsInvariant(this string str, string innerText)
		{
			if (string.IsNullOrWhiteSpace(str))
				return false;
			return str.IndexOf(innerText, StringComparison.InvariantCultureIgnoreCase) != -1;
		}
	}

	public static class Task_ExtensionMethods
	{
		public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
		{
			if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
				await task;
			else
				throw new TimeoutException();
		}
	}
}

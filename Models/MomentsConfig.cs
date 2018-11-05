using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodcastHelper.Models
{
	public class MomentsConfig
	{
		public List<PodcastMoment> Moments { get; set; }

		public MomentsConfig()
		{
			Moments = new List<PodcastMoment>();
		}
	}

	public class PodcastMoment
	{
		public string PodcastShortCode { get; set; }
		public int EpisodeNumber { get; set; }
		public DateTime Time { get; set; }
		public string Description { get; set; }

		public PodcastMoment()
		{
			PodcastShortCode = "";
			EpisodeNumber = -1;
			Time = DateTime.MinValue;
			Description = "";
		}
	}
}

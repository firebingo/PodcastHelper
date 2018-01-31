using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class VlcApi
	{
		private static HttpClient Client = null;

		
		public static async Task PlayFile(string path)
		{
			try
			{

			}
			catch(Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}
	}
}

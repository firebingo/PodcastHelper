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
		private static HttpClient _webClient = null;

		static VlcApi()
		{
			_webClient = new HttpClient();
		}

		public static async Task PlayFile(string path, int? seconds = null)
		{
			try
			{
				await Stop();
				await ClearPlaylist();
				await PlayFile(path);
				if (seconds.HasValue)
					await SeekTo(seconds.Value);
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}

		private static async Task PlayFile(string path)
		{
			try
			{
				if (Uri.TryCreate(new Uri(Config.Instance.ConfigObject.VlcRootUrl), $"/requests/status.xml?command=in_play&input={WebUtility.UrlEncode(path)}", out var uri))
					await SendRequest(uri);
			}
			catch { throw; }
		}

		private static async Task Stop()
		{
			try
			{
				if (Uri.TryCreate(new Uri(Config.Instance.ConfigObject.VlcRootUrl), "/requests/status.xml?command=pl_stop", out var uri))
					await SendRequest(uri);
			}
			catch { throw; }
		}

		private static async Task ClearPlaylist()
		{
			try
			{
				if (Uri.TryCreate(new Uri(Config.Instance.ConfigObject.VlcRootUrl), "/requests/status.xml?command=pl_empty", out var uri))
					await SendRequest(uri);
			}
			catch { throw; }
		}

		private static async Task SeekTo(int seconds)
		{
			try
			{
				if (Uri.TryCreate(new Uri(Config.Instance.ConfigObject.VlcRootUrl), $"/requests/status.xml?command=seek&val={seconds}", out var uri))
					await SendRequest(uri);
			}
			catch { throw; }
		}

		private static async Task SendRequest(Uri uri)
		{
			var request = new HttpRequestMessage();
			request.Method = HttpMethod.Post;
			request.RequestUri = uri;
			var authBase = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Config.Instance.ConfigObject.VlcUsername}:{Config.Instance.ConfigObject.VlcPassword}"));
			request.Headers.Add("Authorization", $"Basic {authBase}");

			var response = await _webClient.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				throw new Exception($"Code: {(int)response.StatusCode} ({Enum.GetName(typeof(HttpStatusCode), response.StatusCode)}) Reason: {response.ReasonPhrase}");
		}
	}
}

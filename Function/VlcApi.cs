﻿using Newtonsoft.Json;
using PodcastHelper.Helpers;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace PodcastHelper.Function
{
	public static class VlcApi
	{
		private static HttpClient _webClient = null;
		private static Thread _progressThread;
		private static bool _runThread = true;
		private static DateTime _nextUpdate = DateTime.MaxValue;
		private static readonly TimeSpan _defaultNextTime = new TimeSpan(0, 1, 0);
		private static readonly TimeSpan _playingNextTime = new TimeSpan(0, 0, 15);

		static VlcApi()
		{
			_webClient = new HttpClient();
			_runThread = true;
			_progressThread = new Thread(RunStatusThread);
			_progressThread.Start();
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
				_nextUpdate = (DateTime.UtcNow + new TimeSpan(0, 0, 5));
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

		private static async Task<string> SendRequest(Uri uri)
		{
			var request = new HttpRequestMessage();
			request.Method = HttpMethod.Post;
			request.RequestUri = uri;
			var authBase = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Config.Instance.ConfigObject.VlcUsername}:{Config.Instance.ConfigObject.VlcPassword}"));
			request.Headers.Add("Authorization", $"Basic {authBase}");

			var response = await _webClient.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				throw new Exception($"Code: {(int)response.StatusCode} ({Enum.GetName(typeof(HttpStatusCode), response.StatusCode)}) Reason: {response.ReasonPhrase}");
			return await response.Content?.ReadAsStringAsync();
		}

		private static async Task UpdateStatus()
		{
			try
			{
				if (Uri.TryCreate(new Uri(Config.Instance.ConfigObject.VlcRootUrl), $"/requests/status.xml", out var uri))
				{
					var statusString = await SendRequest(uri);
					var status = ParseStatus(statusString);
					if (status.FileInfo != null && (status.State == PlayingState.Playing || status.State == PlayingState.Paused))
					{
						PodcastEpisode ep = new PodcastEpisode();
						foreach (var podcast in Config.Instance.EpisodeList.Episodes)
						{
							ep = podcast.Value.Values.FirstOrDefault(x => x.FileName == status.FileInfo.FileName || x.Title == status.FileInfo.FileName);
						}
						if (ep != null)
						{
							if (ep.Progress == null)
								ep.Progress = new EpisodeProgress();

							ep.Progress.Length = status.Length > 0 ? new TimeSpan(0, 0, status.Length) : ep.Progress.Length;
							ep.Progress.Progress = status.Position > 0 ? status.Position : (status.Time / status.Length);
							Config.Instance.SaveConfig();
						}
						//var ep = Config.Instance.EpisodeList.Episodes.Where(x => x.Value.Values.Where(y => y.FileName == status.FileInfo.FileName).First() != null);

						_nextUpdate = DateTime.UtcNow + _playingNextTime;
					}
					else if(status.State == PlayingState.Stopped)
					{
						_nextUpdate = DateTime.UtcNow + _defaultNextTime;
					}
				}
			}
			catch(Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}

		private static void RunStatusThread()
		{
			_nextUpdate = DateTime.UtcNow + _defaultNextTime;
			do
			{
				Thread.Sleep(500);
				if (DateTime.UtcNow > _nextUpdate)
				{
					_nextUpdate = DateTime.MaxValue;
					UpdateStatus().ConfigureAwait(false);
				}
			} while (_runThread);

			return;
		}

		public static void Kill()
		{
			_runThread = false;
		}

		private static VlcStatus ParseStatus(string xmlString)
		{
			var status = new VlcStatus();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xmlString);

			foreach (XmlNode parent in doc.ChildNodes)
			{
				if (parent.Name == "root")
				{
					foreach (XmlNode node in parent.ChildNodes)
					{
						int parseInt = -1;
						switch (node.Name.ToLowerInvariant())
						{
							case "apiversion":
								if (int.TryParse(node.InnerText, out parseInt))
									status.ApiVersion = parseInt;
								break;
							case "time":
								if (int.TryParse(node.InnerText, out parseInt))
									status.Time = parseInt;
								break;
							case "volume":
								if (int.TryParse(node.InnerText, out parseInt))
									status.Volume = parseInt;
								break;
							case "length":
								if (int.TryParse(node.InnerText, out parseInt))
									status.Length = parseInt;
								break;
							case "state":
								status.State = ParseState(node.InnerText);
								break;
							case "version":
								status.Version = node.InnerText;
								break;
							case "position":
								if (double.TryParse(node.InnerText, out double parseDouble))
									status.Position = parseDouble;
								break;
							case "information":
								status.FileInfo = ParseFileInformation(node);
								break;
							default:
								break;
						}
					}
				}
			}

			return status;
		}

		private static PlayingState ParseState(string value)
		{
			switch (value.ToLowerInvariant())
			{
				case "playing":
					return PlayingState.Playing;
				case "paused":
					return PlayingState.Paused;
				case "stopped":
					return PlayingState.Stopped;
				default:
					return PlayingState.Paused;
			}
		}

		private static FileInformation ParseFileInformation(XmlNode iNode)
		{
			var ret = new FileInformation();

			foreach (XmlNode parent in iNode.ChildNodes)
			{
				var nameAtt = HelperMethods.FindXmlAttribute(parent, "name");
				if (nameAtt.ToLowerInvariant() == "meta")
				{
					foreach (XmlNode node in parent.ChildNodes)
					{
						switch (node.Name.ToLowerInvariant())
						{
							case "info":
								var attName = HelperMethods.FindXmlAttribute(node, "name");
								switch(attName.ToLowerInvariant())
								{
									case "filename":
										ret.FileName = node.InnerText;
										break;
									default:
										break;
								}
								break;
							default:
								break;
						}
					}
					break;
				}
			}

			return ret;
		}
	}
}

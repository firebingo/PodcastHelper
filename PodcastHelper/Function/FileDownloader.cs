using PodcastHelper.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class FileDownloader
	{
		private static readonly int _updateProgressMod = 20;
		private static readonly Queue<FileDownloadInfo> _queue;
		private static FileDownloadInfo _downloadingFile;
		private static readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
		private static readonly Thread _doThread;
		private static bool _runThread = true;
		private static readonly HttpClient _webClient;
		private static readonly Memory<byte> _buffer;
		public delegate void onDownloadFinished(bool res, int ep, string shortCode);
		public static event onDownloadFinished OnDownloadFinishedEvent;
		public delegate void onDownloadingUpdate(float progress, int ep, string shortCode);
		public static event onDownloadingUpdate OnDownloadUpdateEvent;

		static FileDownloader()
		{
			_queue = new Queue<FileDownloadInfo>();
			_downloadingFile = null;
			_buffer = new byte[1024 * 16];
			_webClient = new HttpClient();
			//Shouldnt cause any problems for downloading but may make someone watching agents server side double check :)
			_webClient.DefaultRequestHeaders.Add("User-Agent", "NCSA Mosaic/1.0 (X11;SunOS 4.1.4 sun4m)");
			_runThread = true;
			_doThread = new Thread(RunThread);
			_doThread.Name = "FileDownloader";
			_doThread.Priority = ThreadPriority.BelowNormal;
			_doThread.Start();
		}

		public static FileDownloadInfo[] Queue
		{
			get
			{
				return _queue.ToArray();
			}
		}

		private static async void RunThread()
		{
			do
			{
				await Task.Delay(500);
				if (_queue.Count > 0 && _downloadingFile == null)
				{
					_downloadingFile = _queue.Dequeue();
					if (_downloadingFile != null)
					{
						try
						{

							var path = Path.GetDirectoryName(_downloadingFile.FilePath);
							if (!Directory.Exists(path))
								Directory.CreateDirectory(path);

							_ = Task.Run(() => ProcessFileRequest());
						}
						catch (Exception ex)
						{
							_downloadingFile = null;
							OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
							ErrorTracker.CurrentError = ex.Message;
						}
					}
				}
			}
			while (_runThread);

			return;
		}

		public static void Kill()
		{
			_cancelSource.Cancel();
			_runThread = false;
		}

		public static void AddFile(FileDownloadInfo info)
		{
			_queue.Enqueue(info);
		}

		private static async Task ProcessFileRequest()
		{
			try
			{
				var start = DateTime.UtcNow;
				OnDownloadUpdateEvent?.Invoke(0.0f, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
				using var request = new HttpRequestMessage()
				{
					Method = HttpMethod.Get,
					RequestUri = new Uri(_downloadingFile.FileUri)
				};
				using var response = await _webClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancelSource.Token);
				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Download returned a error code: {(int)response.StatusCode} {response.StatusCode}");
				}
				var progressIter = 0;
				_downloadingFile.ContentLength = response.Content.Headers.ContentLength ?? 0;
				using var contentStream = await response.Content.ReadAsStreamAsync(_cancelSource.Token);
				using (var fileStream = new FileStream(_downloadingFile.FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					var bytesRead = 0;
					do
					{
						if (_downloadingFile == null)
							break;
						if ((DateTime.UtcNow - start).TotalSeconds > 45)
							throw new Exception("File took more than 45 seconds to download. Aborting.");

						bytesRead = await contentStream.ReadAsync(_buffer, _cancelSource.Token);
						if (bytesRead == 0)
							break;
						else
						{
							await fileStream.WriteAsync(_buffer, _cancelSource.Token);
							_downloadingFile.ReadBytes += bytesRead;
						}
						try
						{
							if (_downloadingFile.ContentLength != 0 && progressIter++ % _updateProgressMod == 0)
							{
								OnDownloadUpdateEvent?.Invoke((float)_downloadingFile.ReadBytes / _downloadingFile.ContentLength, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
							}
						}
						catch { }
					}
					while (!_cancelSource.Token.IsCancellationRequested);
				}
				//If we shutdown in the middle of downloading delete the file we started
				if (_cancelSource.Token.IsCancellationRequested)
					File.Delete(_downloadingFile.FilePath);
				else
					OnDownloadFinishedEvent?.Invoke(true, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
			}
			catch (Exception ex)
			{
				File.Delete(_downloadingFile.FilePath);
				OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
				ErrorTracker.CurrentError = ex.Message;
			}
			finally
			{
				_downloadingFile = null;
			}
		}
	}

	public class FileDownloadInfo
	{
		public int EpNumber { get; set; }
		public string FilePath { get; set; }
		public string FileUri { get; set; }
		public string PodcastShortCode { get; set; }
		public long ContentLength { get; set; }
		public long ReadBytes { get; set; }
	}
}

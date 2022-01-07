using PodcastHelper.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class FileDownloader
	{
		private static readonly Queue<FileDownloadInfo> _queue;
		private static FileDownloadInfo _downloadingFile;
		private static readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
		private static readonly Thread _doThread;
		private static bool _runThread = true;
		private static readonly HttpClient _webClient;
		private static readonly Memory<byte> _buffer;
		public delegate void onDownloadFinished(bool res, int ep, string shortCode);
		public static event onDownloadFinished OnDownloadFinishedEvent;
		public delegate void onDownloadingUpdate(string shortCode, int ep, float progress);
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

		private static void RunThread()
		{
			do
			{
				Thread.Sleep(500);
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

							Task.Run(() => RunFileRequest());
						}
						catch (Exception ex)
						{
							_downloadingFile = null;
							OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
							ErrorTracker.CurrentError = ex.Message;
						}
					}
				}
				else if (_downloadingFile != null && _downloadingFile.ContentLength != 0)
				{
					try
					{
						OnDownloadUpdateEvent?.Invoke(_downloadingFile.PodcastShortCode, _downloadingFile.EpNumber, _downloadingFile.ReadBytes / _downloadingFile.ContentLength);
					}
					catch { }
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

		private static async Task RunFileRequest()
		{
			try
			{
				await Task.Run(() => ProcessFileRequest()).TimeoutAfter(45000);

				if (File.Exists(_downloadingFile.FilePath))
					OnDownloadFinishedEvent?.Invoke(true, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
				else
					OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
			}
			catch (Exception ex)
			{
				OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
				ErrorTracker.CurrentError = ex.Message;
			}
			finally
			{
				_downloadingFile = null;
			}
		}

		private static async Task ProcessFileRequest()
		{
			try
			{
				using var response = await _webClient.GetAsync(_downloadingFile.FileUri, _cancelSource.Token);
				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Download returned a error code: {(int)response.StatusCode} {response.StatusCode}");
				}
				using var contentStream = await response.Content.ReadAsStreamAsync(_cancelSource.Token);
				_downloadingFile.ContentLength = contentStream.Length;
				using (var fileStream = new FileStream(_downloadingFile.FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					var bytesRead = 0;
					do
					{
						bytesRead = await contentStream.ReadAsync(_buffer, _cancelSource.Token);
						if (bytesRead == 0)
							break;
						else
						{
							await fileStream.WriteAsync(_buffer, _cancelSource.Token);
							_downloadingFile.ReadBytes += bytesRead;
						}
					}
					while (!_cancelSource.Token.IsCancellationRequested);
				}
				//If we shutdown in the middle of downloading delete the file we started
				if (_cancelSource.Token.IsCancellationRequested)
					File.Delete(_downloadingFile.FilePath);
			}
			catch
			{
				throw;
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

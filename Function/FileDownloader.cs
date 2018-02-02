using PodcastHelper.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class FileDownloader
	{
		private static Queue<FileDownloadInfo> _queue;
		private static FileDownloadInfo _downloadingFile;
		private static Stream _downloadingStream;
		private static Thread _doThread;
		private static bool _runThread = true;
		private static HttpClient _webClient;
		public delegate void onDownloadFinished(bool res, int ep, string shortCode);
		public static event onDownloadFinished OnDownloadFinishedEvent;
		public delegate void onDownloadingUpdate(string shortCode, int ep, float progress);
		public static event onDownloadingUpdate OnDownloadUpdateEvent;

		static FileDownloader()
		{
			_queue = new Queue<FileDownloadInfo>();
			_downloadingFile = null;
			_webClient = new HttpClient();
			//Shouldnt cause any problems for downloading but may make someone watching agents server side double check :)
			_webClient.DefaultRequestHeaders.Add("User-Agent", "NCSA Mosaic/1.0 (X11;SunOS 4.1.4 sun4m)");
			_downloadingStream = null;
			_runThread = true;
			_doThread = new Thread(RunThread);
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

							RunFileRequest().ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
							ErrorTracker.CurrentError = ex.Message;
						}
					}
				}
				else if(_downloadingFile != null && _downloadingStream != null)
				{
					try
					{
						//OnDownloadUpdateEvent?.Invoke(_downloadingFile.PodcastShortCode, _downloadingFile.EpNumber, _downloadingStream.Length / _downloadingStream.Position);
					}
					catch { }
				}
			}
			while (_runThread);

			return;
		}

		public static void Kill()
		{
			_runThread = false;
			if (_downloadingFile != null)
				_doThread.Abort();
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
			catch(Exception ex)
			{
				OnDownloadFinishedEvent?.Invoke(false, _downloadingFile.EpNumber, _downloadingFile.PodcastShortCode);
				ErrorTracker.CurrentError = ex.Message;
			}
			finally
			{
				_downloadingStream = null;
				_downloadingFile = null;
			}
		}

		private static async Task ProcessFileRequest()
		{
			try
			{
				using (_downloadingStream = await _webClient.GetStreamAsync(_downloadingFile.FileUri))
				{
					FileStream fileStream = null;

					using (fileStream = new FileStream(_downloadingFile.FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						await _downloadingStream.CopyToAsync(fileStream);
					}
				}
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
	}
}

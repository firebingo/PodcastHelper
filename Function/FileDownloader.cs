using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class FileDownloader
	{
		private static Queue<FileDownloadInfo> _queue;
		private static FileDownloadInfo _downloadingFile;
		private static Thread _doThread;
		private static bool _runThread = true;
		private static WebClient _webClient;
		public delegate void onDownloadFinished(bool res, int ep, string shortCode);
		public static event onDownloadFinished onDownloadFinishedEvent;

		static FileDownloader()
		{
			_queue = new Queue<FileDownloadInfo>();
			_downloadingFile = null;
			_webClient = new WebClient();
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
				Thread.Sleep(2000);
				if(_queue.Count > 0)
				{
					_downloadingFile = _queue.Dequeue();
					if(_downloadingFile != null)
					{
						try
						{
							var path = Path.GetDirectoryName(_downloadingFile.FilePath);
							if(!Directory.Exists(path))
								Directory.CreateDirectory(path);

							_webClient.DownloadFile(_downloadingFile.FileUri, _downloadingFile.FilePath);
							if (File.Exists(_downloadingFile.FilePath))
								onDownloadFinishedEvent?.Invoke(true, _downloadingFile.epNumber, _downloadingFile.podcastShortCode);
							else
								onDownloadFinishedEvent?.Invoke(false, _downloadingFile.epNumber, _downloadingFile.podcastShortCode);
						}
						catch (Exception ex)
						{
							onDownloadFinishedEvent?.Invoke(false, _downloadingFile.epNumber, _downloadingFile.podcastShortCode);
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
			_runThread = false;
		}

		public static void AddFile(FileDownloadInfo info)
		{
			_queue.Enqueue(info);
		}
	}

	public class FileDownloadInfo
	{
		public int epNumber;
		public string FilePath;
		public string FileUri;
		public string podcastShortCode;
	}
}

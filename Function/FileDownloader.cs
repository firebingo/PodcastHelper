using System;
using System.Collections.Generic;
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

		static FileDownloader()
		{
			_queue = new Queue<FileDownloadInfo>();
			_downloadingFile = null;
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
				Thread.Sleep(5000);
				if(_queue.Count > 0)
				{
					_downloadingFile = _queue.Dequeue();
					if(_downloadingFile != null)
					{

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
		public string FilePath;
		public string FileUri;
	}
}

namespace PodcastHelper.Models
{
	public enum PlayingState
	{
		Paused = 0,
		Playing = 1,
		Stopped = 2,
	}

	public class VlcStatus
	{
		public int ApiVersion { get; set; }
		public int Time { get; set; } //The current time in the file in seconds
		public int Volume { get; set; } //The current vlc volume, 100% = 256
		public int Length { get; set; } //The total length of the playing file in seconds
		public PlayingState State { get; set; } //The current state of the player
		public string Version { get; set; } //The VLC version
		public double Position { get; set; } //The current position in the file, normalized by 1
		public FileInformation FileInfo { get; set; } //The information for the playing file
	}

	public class FileInformation
	{
		public string FileName { get; set; }
		public string ArtworkUrl { get; set; }
	}
}

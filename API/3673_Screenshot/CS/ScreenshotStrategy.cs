namespace StockSharp.Samples.Strategies;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;

public class ScreenshotStrategy : Strategy
{
	private readonly StrategyParam<string> _screenshotName;
	private readonly StrategyParam<int> _screenshotWidth;
	private readonly StrategyParam<int> _screenshotHeight;
	private readonly StrategyParam<string> _outputFolder;

	private CancellationTokenSource? _keyListenerCancellation;
	private Task? _keyListenerTask;
	private bool _inputUnavailableNotified;

	public ScreenshotStrategy()
	{
		_screenshotName = Param(nameof(ScreenshotName), "Unnamed")
			.SetDisplay("Screenshot name", "Base name used for captured images.", "General")
			.SetCanOptimize(false);

		_screenshotWidth = Param(nameof(ScreenshotWidth), 1920)
			.SetGreaterThanZero()
			.SetDisplay("Screenshot width", "Width of the generated PNG in pixels.", "General")
			.SetCanOptimize(false);

		_screenshotHeight = Param(nameof(ScreenshotHeight), 1080)
			.SetGreaterThanZero()
			.SetDisplay("Screenshot height", "Height of the generated PNG in pixels.", "General")
			.SetCanOptimize(false);

		_outputFolder = Param(nameof(OutputFolder), "Screenshots")
			.SetDisplay("Output folder", "Relative folder where screenshots are stored.", "General")
			.SetCanOptimize(false);
	}

	public string ScreenshotName
	{
		get => _screenshotName.Value;
		set => _screenshotName.Value = value;
	}

	public int ScreenshotWidth
	{
		get => _screenshotWidth.Value;
		set => _screenshotWidth.Value = value;
	}

	public int ScreenshotHeight
	{
		get => _screenshotHeight.Value;
		set => _screenshotHeight.Value = value;
	}

	public string OutputFolder
	{
		get => _outputFolder.Value;
		set => _outputFolder.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_inputUnavailableNotified = false;

		_keyListenerCancellation = new CancellationTokenSource();
		_keyListenerTask = Task.Run(() => ListenForKeyAsync(_keyListenerCancellation.Token));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_keyListenerCancellation?.Cancel();

		if (_keyListenerTask != null)
		{
			try
			{
				if (!_keyListenerTask.Wait(TimeSpan.FromSeconds(1)))
					LogWarning("Keyboard listener is still running after timeout.");
			}
			catch (AggregateException ex)
			{
				var message = ex.InnerException?.Message ?? ex.Message;
				LogWarning($"Keyboard listener stopped with error: {message}");
			}
		}

		_keyListenerCancellation?.Dispose();
		_keyListenerCancellation = null;
		_keyListenerTask = null;

		base.OnStopped();
	}

	private async Task ListenForKeyAsync(CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (!Console.KeyAvailable)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
						continue;
					}
				}
				catch (InvalidOperationException)
				{
					NotifyInputUnavailable();
					return;
				}
				catch (IOException)
				{
					NotifyInputUnavailable();
					return;
				}

				ConsoleKeyInfo keyInfo;

				try
				{
					keyInfo = Console.ReadKey(true);
				}
				catch (InvalidOperationException)
				{
					NotifyInputUnavailable();
					return;
				}
				catch (IOException)
				{
					NotifyInputUnavailable();
					return;
				}

				if (keyInfo.Key == ConsoleKey.S)
					CaptureScreenshot();
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when the strategy stops.
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private void CaptureScreenshot()
	{
		try
		{
			var timestamp = CurrentTime != default ? CurrentTime : DateTimeOffset.UtcNow;
			var folder = ResolveOutputFolder();
			Directory.CreateDirectory(folder);

			var baseName = SanitizeFileName(ScreenshotName);
			var fileName = $"{baseName}_{timestamp:yyyyMMdd_HHmmss}.png";
			var fullPath = Path.Combine(folder, fileName);

			var width = Math.Max(1, ScreenshotWidth);
			var height = Math.Max(1, ScreenshotHeight);

			using var bitmap = new Bitmap(width, height);
			using var graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Black);

			var text = $"Placeholder capture at {timestamp:O}\nSecurity: {Security?.Id ?? "N/A"}";

			using var font = new Font(FontFamily.GenericSansSerif, 24f, FontStyle.Bold, GraphicsUnit.Pixel);
			using var brush = new SolidBrush(Color.White);
			var layoutRectangle = new RectangleF(20f, 20f, width - 40f, height - 40f);
			graphics.DrawString(text, font, brush, layoutRectangle);

			bitmap.Save(fullPath, ImageFormat.Png);

			LogInfo($"Saved the screenshot {fullPath}.");
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private string ResolveOutputFolder()
	{
		var folder = OutputFolder;

		if (string.IsNullOrWhiteSpace(folder))
			folder = "Screenshots";

		folder = folder.Trim();

		if (Path.IsPathRooted(folder))
			return folder;

		var baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
		return Path.Combine(baseDirectory, folder);
	}

	private static string SanitizeFileName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "Unnamed";

		var safe = string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

		return string.IsNullOrWhiteSpace(safe) ? "Unnamed" : safe;
	}

	private void NotifyInputUnavailable()
	{
		if (_inputUnavailableNotified)
			return;

		_inputUnavailableNotified = true;
		LogWarning("Console input is not available. Keyboard shortcuts are disabled.");
	}
}

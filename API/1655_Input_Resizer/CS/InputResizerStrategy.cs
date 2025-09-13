using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adds resizable borders to active dialog windows and optionally remembers their size.
/// Ported from MetaTrader4 script "InputResizer".
/// </summary>
public class InputResizerStrategy : Strategy
{
	private readonly StrategyParam<bool> _rememberSize;
	private readonly StrategyParam<bool> _individual;
	private readonly StrategyParam<bool> _initMaximized;
	private readonly StrategyParam<bool> _initCustom;
	private readonly StrategyParam<int> _initX;
	private readonly StrategyParam<int> _initY;
	private readonly StrategyParam<int> _initWidth;
	private readonly StrategyParam<int> _initHeight;
	private readonly StrategyParam<int> _sleepTime;
	private readonly StrategyParam<bool> _weekendMode;

	private readonly Dictionary<string, Rect> _sizes = new();
	private CancellationTokenSource? _cts;

	/// <summary>
	/// Save window size for next time.
	/// </summary>
	public bool RememberSize { get => _rememberSize.Value; set => _rememberSize.Value = value; }

	/// <summary>
	/// Store sizes per window title.
	/// </summary>
	public bool Individual { get => _individual.Value; set => _individual.Value = value; }

	/// <summary>
	/// Start window maximized.
	/// </summary>
	public bool InitMaximized { get => _initMaximized.Value; set => _initMaximized.Value = value; }

	/// <summary>
	/// Use custom size on first run.
	/// </summary>
	public bool InitCustom { get => _initCustom.Value; set => _initCustom.Value = value; }

	/// <summary>
	/// Initial X coordinate.
	/// </summary>
	public int InitX { get => _initX.Value; set => _initX.Value = value; }

	/// <summary>
	/// Initial Y coordinate.
	/// </summary>
	public int InitY { get => _initY.Value; set => _initY.Value = value; }

	/// <summary>
	/// Initial width.
	/// </summary>
	public int InitWidth { get => _initWidth.Value; set => _initWidth.Value = value; }

	/// <summary>
	/// Initial height.
	/// </summary>
	public int InitHeight { get => _initHeight.Value; set => _initHeight.Value = value; }

	/// <summary>
	/// Delay between checks in milliseconds.
	/// </summary>
	public int SleepTime { get => _sleepTime.Value; set => _sleepTime.Value = value; }

	/// <summary>
	/// Run without market data.
	/// </summary>
	public bool WeekendMode { get => _weekendMode.Value; set => _weekendMode.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="InputResizerStrategy"/>.
	/// </summary>
	public InputResizerStrategy()
	{
		_rememberSize = Param(nameof(RememberSize), true)
		.SetDisplay("Remember Size", "Save and restore window size", "General");
		_individual = Param(nameof(Individual), true)
		.SetDisplay("Individual", "Store size per window title", "General");
		_initMaximized = Param(nameof(InitMaximized), false)
		.SetDisplay("Init Maximized", "Start maximized", "General");
		_initCustom = Param(nameof(InitCustom), true)
		.SetDisplay("Init Custom", "Use custom size on start", "General");
		_initX = Param(nameof(InitX), 200)
		.SetDisplay("Init X", "Initial left coordinate", "General");
		_initY = Param(nameof(InitY), 200)
		.SetDisplay("Init Y", "Initial top coordinate", "General");
		_initWidth = Param(nameof(InitWidth), 350)
		.SetDisplay("Init Width", "Initial width", "General");
		_initHeight = Param(nameof(InitHeight), 450)
		.SetDisplay("Init Height", "Initial height", "General");
		_sleepTime = Param(nameof(SleepTime), 300)
		.SetDisplay("Sleep Time", "Delay in ms between checks", "General");
		_weekendMode = Param(nameof(WeekendMode), false)
		.SetDisplay("Weekend Mode", "Run even without market data", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cts = new CancellationTokenSource();
		_ = Task.Run(() => MonitorAsync(_cts.Token));

		if (!WeekendMode)
			Stop();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);
		_cts?.Cancel();
	}

	private async Task MonitorAsync(CancellationToken token)
	{
		var last = IntPtr.Zero;

		while (!token.IsCancellationRequested)
		{
			var wnd = GetForegroundWindow();

			if (wnd != IntPtr.Zero && wnd != last)
			{
				ProcessWindow(wnd);
				last = wnd;
			}

			try
			{
				await Task.Delay(SleepTime, token).ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
			}
		}
	}

	private void ProcessWindow(IntPtr wnd)
	{
		var style = GetWindowLong(wnd, GWL_STYLE);
		style |= WS_SIZEBOX | WS_MAXIMIZEBOX | WS_MINIMIZEBOX;
		SetWindowLong(wnd, GWL_STYLE, style);

		var sb = new StringBuilder(256);
		GetWindowText(wnd, sb, sb.Capacity);
		var key = Individual ? sb.ToString() : "global";

		if (RememberSize && _sizes.TryGetValue(key, out var rect))
		{
			SetWindowPos(wnd, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, SWP_NOZORDER | SWP_NOACTIVATE);
		}
		else
		{
			if (InitMaximized)
				ShowWindow(wnd, SW_MAXIMIZE);
			else if (InitCustom)
				SetWindowPos(wnd, IntPtr.Zero, InitX, InitY, InitWidth, InitHeight, SWP_NOZORDER | SWP_NOACTIVATE);
		}

		if (RememberSize)
		{
			GetWindowRect(wnd, out rect);
			_sizes[key] = rect;
		}
	}

	#region WinAPI

	private const int GWL_STYLE = -16;
	private const int WS_SIZEBOX = 0x00040000;
	private const int WS_MINIMIZEBOX = 0x00020000;
	private const int WS_MAXIMIZEBOX = 0x00010000;
	private const int SW_MAXIMIZE = 3;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_NOACTIVATE = 0x0010;

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	#endregion
}

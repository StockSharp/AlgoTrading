using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the original MQL4 Test_ChartEvent script.
/// It creates two virtual buttons, simulates keyboard driven moves,
/// fires random custom events, and tracks mouse move sequences.
/// </summary>
public class TestChartEventStrategy : Strategy
{
	private enum CustomEventType
	{
		Event1 = 0,
		Event2 = 1,
		Broadcast = 2,
	}

	private readonly StrategyParam<int> _logLevel;
	private readonly StrategyParam<int> _moveStep;
	private readonly StrategyParam<int> _canvasWidth;
	private readonly StrategyParam<int> _canvasHeight;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();
	private readonly MouseEventEmulator _mouse = new();

	private ChartButton? _firstButton;
	private ChartButton? _secondButton;
	private ChartButton? _selectedButton;
	private bool _infoPrinted;

	/// <summary>
	/// Controls the amount of diagnostic messages.
	/// </summary>
	public int LogLevel
	{
		get => _logLevel.Value;
		set => _logLevel.Value = value;
	}

	/// <summary>
	/// Pixels added per key press.
	/// </summary>
	public int MoveStep
	{
		get => _moveStep.Value;
		set => _moveStep.Value = value;
	}

	/// <summary>
	/// Width of the simulated chart canvas in pixels.
	/// </summary>
	public int CanvasWidth
	{
		get => _canvasWidth.Value;
		set => _canvasWidth.Value = value;
	}

	/// <summary>
	/// Height of the simulated chart canvas in pixels.
	/// </summary>
	public int CanvasHeight
	{
		get => _canvasHeight.Value;
		set => _canvasHeight.Value = value;
	}

	/// <summary>
	/// Candle type that drives timer-like callbacks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TestChartEventStrategy"/> class.
	/// </summary>
	public TestChartEventStrategy()
	{
		_logLevel = Param(nameof(LogLevel), 1)
		.SetDisplay("Log Level", "Controls verbosity of diagnostic output", "Diagnostics")
		.SetCanOptimize(true)
		.SetOptimize(0, 2, 1);

		_moveStep = Param(nameof(MoveStep), 10)
		.SetGreaterThanZero()
		.SetDisplay("Move Step", "Horizontal and vertical step in pixels", "Objects")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_canvasWidth = Param(nameof(CanvasWidth), 640)
		.SetGreaterThanZero()
		.SetDisplay("Canvas Width", "Width of the virtual chart surface", "Objects");

		_canvasHeight = Param(nameof(CanvasHeight), 360)
		.SetGreaterThanZero()
		.SetDisplay("Canvas Height", "Height of the virtual chart surface", "Objects");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Series used to emulate timer behaviour", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstButton = null;
		_secondButton = null;
		_selectedButton = null;
		_infoPrinted = false;
		_mouse.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		CreateButtons();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		if (LogLevel > 0)
		LogInfo("Strategy started. Help is printed automatically when the first candle arrives.");
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		_mouse.Reset();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_infoPrinted)
		{
			PrintHelp();
			PrintInfo();
			_infoPrinted = true;
		}

		TriggerTimerEvent(candle);
		SimulateKeyMovement();
		HandleMouse(candle);
	}

	private void TriggerTimerEvent(ICandleMessage candle)
	{
		var eventType = (CustomEventType)_random.Next(0, 3);
		var suffix = GetTimeFrameSuffix();
		var securityId = Security?.Id ?? string.Empty;
		var sparam = $"by {securityId} {suffix}";

		if (LogLevel > 0)
		LogInfo($"Custom event triggered: {eventType} ({sparam}).");

		switch (eventType)
		{
			case CustomEventType.Event1:
			if (LogLevel > 1)
			LogInfo("Custom Event #1 processed.");
			break;
			case CustomEventType.Event2:
			if (LogLevel > 1)
			LogInfo("Custom Event #2 processed.");
			break;
			case CustomEventType.Broadcast:
			BroadcastEvent(sparam);
			break;
		}
	}

	private void BroadcastEvent(string sparam)
	{
		var charts = _random.Next(2, 5);

		for (var i = 0; i < charts; i++)
		{
			if (LogLevel > 0)
			LogInfo($"Broadcast event delivered to chart #{i + 1}: {sparam}.");
		}
	}

	private void SimulateKeyMovement()
	{
		if (_selectedButton == null)
		return;

		if (_random.NextDouble() > 0.35)
		return;

		var hSens = _random.Next(-1, 2);
		var vSens = _random.Next(-1, 2);

		if (hSens == 0 && vSens == 0)
		return;

		_selectedButton.Move(hSens, vSens, CanvasWidth, CanvasHeight);

		if (LogLevel > 0)
		LogInfo($"Object '{_selectedButton.Name}' moved to ({_selectedButton.X}, {_selectedButton.Y}).");

		if (_random.NextDouble() < 0.25)
		SwitchSelection();
	}

	private void SwitchSelection()
	{
		if (_selectedButton == null)
		return;

		_selectedButton = _selectedButton == _firstButton ? _secondButton : _firstButton;

		if (LogLevel > 0 && _selectedButton != null)
		LogInfo($"Selected object changed to '{_selectedButton.Name}'.");
	}

	private void HandleMouse(ICandleMessage candle)
	{
		if (!_mouse.Enabled)
		{
			if (_random.NextDouble() < 0.2)
			{
				_mouse.Enable();

				if (LogLevel > 0)
				LogInfo("Mouse move mode activated.");
			}

			return;
		}

		if (!_mouse.IsPressRegistered)
		{
			var startX = _random.Next(0, Math.Max(1, CanvasWidth));
			var startY = _random.Next(0, Math.Max(1, CanvasHeight));
			var pressTime = candle.OpenTime;
			var pressPrice = candle.OpenPrice;

			_mouse.RegisterPress(startX, startY, pressTime, pressPrice);

			if (LogLevel > 1)
			LogInfo($"Mouse press registered at ({startX}, {startY}).");

			return;
		}

		var releaseX = _random.Next(0, Math.Max(1, CanvasWidth));
		var releaseY = _random.Next(0, Math.Max(1, CanvasHeight));
		var releaseTime = GetCloseTime(candle);
		var releasePrice = candle.ClosePrice;

		var result = _mouse.RegisterRelease(releaseX, releaseY, releaseTime, releasePrice);

		if (result.Moved)
		{
			if (LogLevel > 0)
			{
				LogInfo($"Mouse moved from ({result.StartX}, {result.StartY}) to ({result.EndX}, {result.EndY}) between {result.StartTime:O} and {result.EndTime:O}. Prices: {result.StartPrice} -> {result.EndPrice}.");
			}
		}
		else if (LogLevel > 1)
		{
			LogInfo("Mouse movement ignored (displacement or duration too small).");
		}

		_mouse.Disable();
	}

	private void CreateButtons()
	{
		var width = 120;
		var height = 50;

		_firstButton = new ChartButton("Green button", "Green", MoveStep, width, height, CanvasWidth, CanvasHeight, _random);
		_secondButton = new ChartButton("Yellow button", "Chocolate", MoveStep, width, height, CanvasWidth, CanvasHeight, _random);
		_selectedButton = _firstButton;

		if (LogLevel > 0)
		LogInfo($"Objects created. Selected '{_selectedButton?.Name}'.");
	}

	private void PrintHelp()
	{
		if (LogLevel == 0)
		return;

		LogInfo("Help: virtual objects react to simulated keyboard directions and mouse drags.");
		LogInfo("Help: random custom events reproduce EventChartCustom() calls.");
		LogInfo("Help: mouse mode toggles automatically in this emulation.");
	}

	private void PrintInfo()
	{
		if (LogLevel == 0)
		return;

		if (_firstButton != null)
		LogInfo(_firstButton.GetInfo());

		if (_secondButton != null)
		LogInfo(_secondButton.GetInfo());
	}

	private DateTimeOffset GetCloseTime(ICandleMessage candle)
	{
		if (candle.CloseTime != default)
		return candle.CloseTime;

		if (CandleType.Arg is TimeSpan span)
		return candle.OpenTime + span;

		return candle.OpenTime;
	}

	private string GetTimeFrameSuffix()
	{
		return CandleType.Arg is TimeSpan span
		? span.ToString()
		: CandleType.ToString();
	}

	private sealed class ChartButton
	{
		public ChartButton(string name, string color, int step, int width, int height, int canvasWidth, int canvasHeight, Random random)
		{
			Name = name;
			Color = color;
			Step = step;
			Width = width;
			Height = height;

			SetRandomPosition(canvasWidth, canvasHeight, random);
		}

		public string Name { get; }

		public string Color { get; }

		public int Step { get; }

		public int Width { get; }

		public int Height { get; }

		public int X { get; private set; }

		public int Y { get; private set; }

		public void Move(int hSens, int vSens, int canvasWidth, int canvasHeight)
		{
			if (hSens == 0 && vSens == 0)
			return;

			var newX = X + Step * hSens;
			var newY = Y + Step * vSens;

			if (newX < 0)
			newX = 0;

			if (newY < 0)
			newY = 0;

			if (newX > canvasWidth - Width)
			newX = canvasWidth - Width;

			if (newY > canvasHeight - Height)
			newY = canvasHeight - Height;

			X = newX;
			Y = newY;
		}

		public string GetInfo()
		{
			return $"Object '{Name}' (color: {Color}) at ({X}, {Y}).";
		}

		private void SetRandomPosition(int canvasWidth, int canvasHeight, Random random)
		{
			var horizontalRange = Math.Max(1, canvasWidth - Width);
			var verticalRange = Math.Max(1, canvasHeight - Height);

			X = random.Next(0, horizontalRange);
			Y = random.Next(0, verticalRange);
		}
	}

	private sealed class MouseEventEmulator
	{
		private const int MinPixels = 2;
		private static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(460);

		public bool Enabled { get; private set; }

		public bool IsPressRegistered { get; private set; }

		public int StartX { get; private set; }

		public int StartY { get; private set; }

		public DateTimeOffset StartTime { get; private set; }

		public decimal StartPrice { get; private set; }

		public void Enable()
		{
			Enabled = true;
			IsPressRegistered = false;
		}

		public void Disable()
		{
			Enabled = false;
			IsPressRegistered = false;
			StartX = 0;
			StartY = 0;
			StartTime = default;
			StartPrice = 0;
		}

		public void Reset()
		{
			Disable();
		}

		public void RegisterPress(int x, int y, DateTimeOffset time, decimal price)
		{
			if (!Enabled)
			return;

			StartX = x;
			StartY = y;
			StartTime = time;
			StartPrice = price;
			IsPressRegistered = true;
		}

		public MouseMoveResult RegisterRelease(int x, int y, DateTimeOffset time, decimal price)
		{
			if (!Enabled || !IsPressRegistered)
			return MouseMoveResult.Empty;

			var moved = Math.Abs(x - StartX) > MinPixels || Math.Abs(y - StartY) > MinPixels;

			if (time - StartTime < MinDuration)
			moved = false;

			IsPressRegistered = false;

			return new MouseMoveResult(moved, StartX, StartY, x, y, StartTime, time, StartPrice, price);
		}
	}

	private readonly record struct MouseMoveResult(bool Moved, int StartX, int StartY, int EndX, int EndY, DateTimeOffset StartTime, DateTimeOffset EndTime, decimal StartPrice, decimal EndPrice)
	{
		public static readonly MouseMoveResult Empty = new(false, 0, 0, 0, 0, default, default, 0, 0);
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the Open Oscillator Cloud MMRec expert advisor using the high level API.
/// </summary>
public class OpenOscillatorCloudMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private LengthIndicator<decimal> _upSmoother = null!;
	private LengthIndicator<decimal> _downSmoother = null!;

	private readonly List<ICandleMessage> _window = new();
	private readonly List<decimal> _upHistory = new();
	private readonly List<decimal> _downHistory = new();

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public SmoothingMethod Smoothing { get => _smoothingMethod.Value; set => _smoothingMethod.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public bool EnableLongEntry { get => _enableLongEntry.Value; set => _enableLongEntry.Value = value; }
	public bool EnableShortEntry { get => _enableShortEntry.Value; set => _enableShortEntry.Value = value; }
	public bool EnableLongExit { get => _enableLongExit.Value; set => _enableLongExit.Value = value; }
	public bool EnableShortExit { get => _enableShortExit.Value; set => _enableShortExit.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	public OpenOscillatorCloudMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe for oscillator calculations", "General");

		_period = Param(nameof(Period), 20)
		.SetDisplay("Oscillator Period", "Lookback window used to locate the highest high and lowest low", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Simple)
		.SetDisplay("Smoothing Method", "Moving average used to smooth the open gaps", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 10)
		.SetDisplay("Smoothing Length", "Period for the smoothing moving average", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of fully closed bars to delay the signal", "Indicator")
		.SetNotNegative();

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions on bullish cross", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions on bearish cross", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
		.SetDisplay("Enable Long Exits", "Close long positions when oscillator turns bearish", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
		.SetDisplay("Enable Short Exits", "Close short positions when oscillator turns bullish", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Order volume used for market entries", "Trading")
		.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss Points", "Protective stop in price steps (0 disables the stop)", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit Points", "Profit target in price steps (0 disables the target)", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_window.Clear();
		_upHistory.Clear();
		_downHistory.Clear();
		ResetRiskLevels();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_upSmoother = CreateSmoother();
		_downSmoother = CreateSmoother();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _upSmoother);
			DrawIndicator(area, _downSmoother);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position == 0)
		// Clear any stale protective levels once the position is flat.
		ResetRiskLevels();

		// Maintain a rolling window of finished candles for oscillator calculations.
		_window.Add(candle);
		if (_window.Count > Period)
		_window.RemoveAt(0);

		if (_window.Count < Period)
		return;

		// Locate the opens of the candles with the highest high and lowest low.
		var extremes = GetExtremes();
		var rawUp = candle.OpenPrice - extremes.highestOpen;
		var rawDown = extremes.lowestOpen - candle.OpenPrice;

		// Smooth the raw gaps using the configured moving averages.
		var upValue = _upSmoother.Process(rawUp, candle.CloseTime, true).ToDecimal();
		var downValue = _downSmoother.Process(rawDown, candle.CloseTime, true).ToDecimal();

		if (!_upSmoother.IsFormed || !_downSmoother.IsFormed)
		return;

		// Track smoothed values so that delayed signals can be evaluated.
		_upHistory.Add(upValue);
		_downHistory.Add(downValue);

		var maxHistory = Math.Max(SignalBar + 3, Period);
		if (_upHistory.Count > maxHistory)
		{
			var removeCount = _upHistory.Count - maxHistory;
			_upHistory.RemoveRange(0, removeCount);
			_downHistory.RemoveRange(0, removeCount);
		}

		var currentIndex = _upHistory.Count - 1 - SignalBar;
		if (currentIndex < 0)
		return;

		var prevIndex = currentIndex - 1;
		if (prevIndex < 0)
		return;

		var prevUp = _upHistory[prevIndex];
		var prevDown = _downHistory[prevIndex];
		var currentUp = _upHistory[currentIndex];
		var currentDown = _downHistory[currentIndex];

		// Derive trading signals from delayed oscillator values.
		var closeShort = EnableShortExit && prevUp > prevDown;
		var closeLong = EnableLongExit && prevUp < prevDown;
		var openLong = EnableLongEntry && prevUp > prevDown && currentUp <= currentDown;
		var openShort = EnableShortEntry && prevUp < prevDown && currentUp >= currentDown;

		// Exit immediately if a protective level was touched on this bar.
		if (CheckProtectiveTargets(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position < 0 && closeShort)
		{
			CancelActiveOrders();
			ClosePosition();
			ResetRiskLevels();
		}

		if (Position > 0 && closeLong)
		{
			CancelActiveOrders();
			ClosePosition();
			ResetRiskLevels();
		}

		if (openLong && Position <= 0)
		{
			if (Position < 0)
			{
				CancelActiveOrders();
				ClosePosition();
			}

			BuyMarket();
			SetProtectiveLevels(candle.ClosePrice, true);
		}

		if (openShort && Position >= 0)
		{
			if (Position > 0)
			{
				CancelActiveOrders();
				ClosePosition();
			}

			SellMarket();
			SetProtectiveLevels(candle.ClosePrice, false);
		}
	}

	private (decimal highestOpen, decimal lowestOpen) GetExtremes()
	{
		// Scan the rolling window to find the opens corresponding to price extremes.
		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;
		var highestOpen = _window[0].OpenPrice;
		var lowestOpen = _window[0].OpenPrice;

		for (var i = 0; i < _window.Count; i++)
		{
			var c = _window[i];

			if (c.HighPrice >= highestHigh)
			{
				highestHigh = c.HighPrice;
				highestOpen = c.OpenPrice;
			}

			if (c.LowPrice <= lowestLow)
			{
				lowestLow = c.LowPrice;
				lowestOpen = c.OpenPrice;
			}
		}

		return (highestOpen, lowestOpen);
	}

	private bool CheckProtectiveTargets(ICandleMessage candle)
	{
		// Validate whether stop-loss or take-profit levels were hit on the latest candle.
		var triggered = false;
		var stepCheck = Security?.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value + stepCheck / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
			else if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value - stepCheck / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value - stepCheck / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
			else if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value + stepCheck / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
		}

		return triggered;
	}

	private void SetProtectiveLevels(decimal entryPrice, bool isLong)
	{
		// Translate distances expressed in points to absolute price levels.
		var step = Security?.PriceStep ?? 1m;

		if (isLong)
		{
			_longStop = StopLossPoints > 0 ? entryPrice - StopLossPoints * step : null;
			_longTarget = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * step : null;
			_shortStop = null;
			_shortTarget = null;
		}
		else
		{
			_shortStop = StopLossPoints > 0 ? entryPrice + StopLossPoints * step : null;
			_shortTarget = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * step : null;
			_longStop = null;
			_longTarget = null;
		}
	}

	private void ResetRiskLevels()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	private LengthIndicator<decimal> CreateSmoother()
	{
		// Build the moving average instance that will smooth the oscillator values.
		return Smoothing switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = SmoothingLength },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = SmoothingLength },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = SmoothingLength },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = SmoothingLength },
			_ => throw new ArgumentOutOfRangeException(nameof(Smoothing), Smoothing, "Unsupported smoothing method."),
};
	}

public enum SmoothingMethod
{
	Simple,
	Exponential,
	Smoothed,
	Weighted,
}
}

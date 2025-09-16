namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades on normalized ATR histogram color transitions.
/// </summary>
public class AtrNormalizeHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<AtrNormalizeSmoothingMethod> _firstSmoothingMethod;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<AtrNormalizeSmoothingMethod> _secondSmoothingMethod;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private IIndicator _diffSmoother = null!;
	private IIndicator _rangeSmoother = null!;
	private readonly List<int> _colorHistory = new();
	private decimal? _previousClose;
	private decimal? _entryPrice;

	/// <summary>
	/// Available smoothing methods for the normalized ATR components.
	/// </summary>
	public enum AtrNormalizeSmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Jurik
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public AtrNormalizeSmoothingMethod FirstSmoothingMethod { get => _firstSmoothingMethod.Value; set => _firstSmoothingMethod.Value = value; }
	public int FirstLength { get => _firstLength.Value; set => _firstLength.Value = value; }
	public AtrNormalizeSmoothingMethod SecondSmoothingMethod { get => _secondSmoothingMethod.Value; set => _secondSmoothingMethod.Value = value; }
	public int SecondLength { get => _secondLength.Value; set => _secondLength.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal MiddleLevel { get => _middleLevel.Value; set => _middleLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public bool EnableBuyEntries { get => _enableBuyEntries.Value; set => _enableBuyEntries.Value = value; }
	public bool EnableSellEntries { get => _enableSellEntries.Value; set => _enableSellEntries.Value = value; }
	public bool EnableBuyExits { get => _enableBuyExits.Value; set => _enableBuyExits.Value = value; }
	public bool EnableSellExits { get => _enableSellExits.Value; set => _enableSellExits.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	public AtrNormalizeHistogramStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_firstSmoothingMethod = Param(nameof(FirstSmoothingMethod), AtrNormalizeSmoothingMethod.Simple)
			.SetDisplay("Diff Smoothing", "Smoother for close-low difference", "Indicator")
			.SetCanOptimize(true);

		_firstLength = Param(nameof(FirstLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Diff Length", "Length for difference smoothing", "Indicator")
			.SetCanOptimize(true);

		_secondSmoothingMethod = Param(nameof(SecondSmoothingMethod), AtrNormalizeSmoothingMethod.Simple)
			.SetDisplay("Range Smoothing", "Smoother for true range", "Indicator")
			.SetCanOptimize(true);

		_secondLength = Param(nameof(SecondLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Length for range smoothing", "Indicator")
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper histogram threshold", "Levels")
			.SetCanOptimize(true);

		_middleLevel = Param(nameof(MiddleLevel), 50m)
			.SetDisplay("Middle Level", "Neutral histogram threshold", "Levels")
			.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Lower histogram threshold", "Levels")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Offset in bars for signal evaluation", "Signals");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Signals");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Signals");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Long Exits", "Close longs on bearish colors", "Signals");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Short Exits", "Close shorts on bullish colors", "Signals");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (pts)", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (pts)", "Protective target distance in points", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_diffSmoother = CreateSmoother(FirstSmoothingMethod, FirstLength);
		_rangeSmoother = CreateSmoother(SecondSmoothingMethod, SecondLength);

		_colorHistory.Clear();
		_previousClose = null;
		_entryPrice = null;

		Volume = TradeVolume;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _previousClose ?? candle.ClosePrice;
		var diff = candle.ClosePrice - candle.LowPrice;
		var range = Math.Max(candle.HighPrice, prevClose) - Math.Min(candle.LowPrice, prevClose);

		// Smooth the close-low difference and true range to match the indicator behavior.
		var diffValue = _diffSmoother.Process(diff).GetValue<decimal>();
		var rangeValue = _rangeSmoother.Process(range).GetValue<decimal>();

		_previousClose = candle.ClosePrice;

		if (!_diffSmoother.IsFormed || !_rangeSmoother.IsFormed)
			return;

		var priceStep = GetPriceStep();
		var safeRange = Math.Max(Math.Abs(rangeValue), priceStep);
		if (safeRange == 0m)
			return;

		var normalized = 100m * diffValue / safeRange;
		var color = GetColor(normalized);

		_colorHistory.Add(color);
		var maxHistory = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > maxHistory)
		_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);

		ApplyProtection(candle);
		EvaluateSignals(candle);
	}

	private void ApplyProtection(ICandleMessage candle)
	{
		if (_entryPrice is null)
		return;

		var entry = _entryPrice.Value;
		var stopOffset = PointsToPrice(StopLossPoints);
		var takeOffset = PointsToPrice(TakeProfitPoints);
		var closed = false;

		// Close longs if stop loss or take profit boundaries are breached intrabar.
		if (Position > 0)
		{
		if (StopLossPoints > 0m && candle.LowPrice <= entry - stopOffset)
		{
		ClosePosition();
		closed = true;
		}
		else if (TakeProfitPoints > 0m && candle.HighPrice >= entry + takeOffset)
		{
		ClosePosition();
		closed = true;
		}
		}
		// Close shorts if protective levels are touched.
		else if (Position < 0)
		{
		if (StopLossPoints > 0m && candle.HighPrice >= entry + stopOffset)
		{
		ClosePosition();
		closed = true;
		}
		else if (TakeProfitPoints > 0m && candle.LowPrice <= entry - takeOffset)
		{
		ClosePosition();
		closed = true;
		}
		}

		if (closed || Position == 0)
		_entryPrice = null;
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		if (_colorHistory.Count <= SignalBar)
		return;

		var currentIndex = _colorHistory.Count - SignalBar;
		var previousIndex = currentIndex - 1;

		if (previousIndex < 0 || currentIndex >= _colorHistory.Count)
		return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];

		var buyEntrySignal = EnableBuyEntries && previousColor == 0 && currentColor != 0;
		var sellEntrySignal = EnableSellEntries && previousColor == 4 && currentColor != 4;
		var buyExitSignal = EnableBuyExits && previousColor == 4;
		var sellExitSignal = EnableSellExits && previousColor == 0;

		// Exit conditions are processed before opening new trades.
		if (buyExitSignal && Position > 0)
		{
		ClosePosition();
		_entryPrice = null;
		}

		if (sellExitSignal && Position < 0)
		{
		ClosePosition();
		_entryPrice = null;
		}

		if (buyEntrySignal && Position <= 0)
		{
		var volume = TradeVolume + (Position < 0 ? -Position : 0m);
		if (volume > 0m)
		{
		CancelActiveOrders();
		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		}
		}
		else if (sellEntrySignal && Position >= 0)
		{
		var volume = TradeVolume + (Position > 0 ? Position : 0m);
		if (volume > 0m)
		{
		CancelActiveOrders();
		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		}
		}
	}

	private int GetColor(decimal normalized)
	{
		if (normalized > HighLevel)
		return 0;

		if (normalized > MiddleLevel)
		return 1;

		if (normalized < LowLevel)
		return 4;

		if (normalized < MiddleLevel)
		return 3;

		return 2;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is null or <= 0m ? 1m : step.Value;
	}

	private decimal PointsToPrice(decimal points)
	{
		if (points <= 0m)
		return 0m;

		return points * GetPriceStep();
	}

	private IIndicator CreateSmoother(AtrNormalizeSmoothingMethod method, int length)
	{
		if (length <= 0)
		throw new ArgumentOutOfRangeException(nameof(length));

		return method switch
		{
		AtrNormalizeSmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
		AtrNormalizeSmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
		AtrNormalizeSmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
		AtrNormalizeSmoothingMethod.Jurik => new JurikMovingAverage { Length = length },
		_ => new SimpleMovingAverage { Length = length },
		};
	}
}

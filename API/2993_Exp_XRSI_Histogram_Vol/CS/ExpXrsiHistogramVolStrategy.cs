using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume-weighted RSI histogram breakout strategy converted from the original MQL implementation.
/// </summary>
public class ExpXrsiHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _mm1;
	private readonly StrategyParam<decimal> _mm2;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<VolumeCalculationMode> _volumeMode;
	private readonly StrategyParam<int> _highLevel2;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<int> _lowLevel2;
	private readonly StrategyParam<XrsiSmoothingMethod> _maMethod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private RelativeStrengthIndex _rsi = null!;
	private IIndicator _histogramSmoother = null!;
	private IIndicator _volumeSmoother = null!;
	private readonly List<int> _colorHistory = new();

	public ExpXrsiHistogramVolStrategy()
	{
		_mm1 = Param(nameof(Mm1), 0.1m)
		.SetDisplay("First MM", "Multiplier for the first entry volume", "Money Management");
		_mm2 = Param(nameof(Mm2), 0.2m)
		.SetDisplay("Second MM", "Multiplier for the second entry volume", "Money Management");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading Switches");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading Switches");
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Allow Long Exits", "Enable closing existing long positions", "Trading Switches");
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Allow Short Exits", "Enable closing existing short positions", "Trading Switches");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for indicator calculations", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicator");
		_volumeMode = Param(nameof(VolumeMode), VolumeCalculationMode.Tick)
		.SetDisplay("Volume Mode", "Use tick or real volume for weighting", "Indicator");
		_highLevel2 = Param(nameof(HighLevel2), 17)
		.SetDisplay("High Level 2", "Upper histogram multiplier for the strongest bullish state", "Indicator");
		_highLevel1 = Param(nameof(HighLevel1), 5)
		.SetDisplay("High Level 1", "Upper histogram multiplier for the moderate bullish state", "Indicator");
		_lowLevel1 = Param(nameof(LowLevel1), -5)
		.SetDisplay("Low Level 1", "Lower histogram multiplier for the moderate bearish state", "Indicator");
		_lowLevel2 = Param(nameof(LowLevel2), -17)
		.SetDisplay("Low Level 2", "Lower histogram multiplier for the strongest bearish state", "Indicator");
		_maMethod = Param(nameof(MaMethod), XrsiSmoothingMethod.Sma)
		.SetDisplay("Smoothing Method", "Moving average method applied to RSI*volume", "Indicator");
		_maLength = Param(nameof(MaLength), 12)
		.SetDisplay("Smoothing Length", "Number of periods used for smoothing", "Indicator");
		_maPhase = Param(nameof(MaPhase), 15)
		.SetDisplay("MA Phase", "Phase parameter for advanced moving averages", "Indicator");
		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars back to evaluate signals", "Trading Logic");
		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss (points)", "Protective stop in price steps", "Risk");
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit (points)", "Protective target in price steps", "Risk");
	}

	public decimal Mm1
	{
		get => _mm1.Value;
		set => _mm1.Value = value;
	}

	public decimal Mm2
	{
		get => _mm2.Value;
		set => _mm2.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public VolumeCalculationMode VolumeMode
	{
		get => _volumeMode.Value;
		set => _volumeMode.Value = value;
	}

	public int HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	public int HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	public int LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	public int LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	public XrsiSmoothingMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public int MaPhase
	{
		get => _maPhase.Value;
		set => _maPhase.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_histogramSmoother = CreateSmoother(MaMethod, MaLength);
		_volumeSmoother = CreateSmoother(MaMethod, MaLength);
		_colorHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var priceStep = Security?.PriceStep ?? 0m;
		Unit stopLoss = null;
		Unit takeProfit = null;

		if (StopLossPoints > 0 && priceStep > 0m)
		{
			stopLoss = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);
		}

		if (TakeProfitPoints > 0 && priceStep > 0m)
		{
			takeProfit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
		}

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var rsiValue = _rsi.Process(candle);
		if (!rsiValue.IsFinal)
		return;

		var rsi = rsiValue.GetValue<decimal>();
		var volume = GetWeightedVolume(candle);
		if (volume <= 0m)
		volume = 1m;

		var histogramSource = (rsi - 50m) * volume;

		var histogramValue = _histogramSmoother.Process(new DecimalIndicatorValue(_histogramSmoother, histogramSource, candle.OpenTime, true));
		var volumeValue = _volumeSmoother.Process(new DecimalIndicatorValue(_volumeSmoother, volume, candle.OpenTime, true));

		if (!histogramValue.IsFinal || !volumeValue.IsFinal)
		return;

		var histogram = histogramValue.ToDecimal();
		var smoothedVolume = volumeValue.ToDecimal();

		var upper2 = HighLevel2 * smoothedVolume;
		var upper1 = HighLevel1 * smoothedVolume;
		var lower1 = LowLevel1 * smoothedVolume;
		var lower2 = LowLevel2 * smoothedVolume;

		var color = DetermineColor(histogram, upper1, upper2, lower1, lower2);
		UpdateColorHistory(color);

		if (!TryGetColors(out var currentColor, out var previousColor))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (SellPosClose && (currentColor == 0 || currentColor == 1) && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (BuyPosClose && (currentColor == 3 || currentColor == 4) && Position > 0)
		{
			SellMarket(Position);
		}

		var baseVolume = Volume > 0m ? Volume : 1m;
		var longVolume1 = Math.Max(baseVolume * Mm1, 0m);
		var longVolume2 = Math.Max(baseVolume * Mm2, 0m);

		if (BuyPosOpen && currentColor == 1 && previousColor > 1)
		{
			ExecuteLong(longVolume1);
		}

		if (BuyPosOpen && currentColor == 0 && previousColor > 0)
		{
			ExecuteLong(longVolume2);
		}

		if (SellPosOpen && currentColor == 3 && previousColor < 3)
		{
			ExecuteShort(longVolume1);
		}

		if (SellPosOpen && currentColor == 4 && previousColor < 4)
		{
			ExecuteShort(longVolume2);
		}
	}

	private void ExecuteLong(decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		BuyMarket(volume);
	}

	private void ExecuteShort(decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position > 0)
		{
			SellMarket(Position);
		}

		SellMarket(volume);
	}

	private void UpdateColorHistory(int color)
	{
		_colorHistory.Add(color);
		var maxHistory = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > maxHistory)
		{
			_colorHistory.RemoveAt(0);
		}
	}

	private bool TryGetColors(out int currentColor, out int previousColor)
	{
		currentColor = 0;
		previousColor = 0;

		var offset = Math.Max(1, SignalBar);
		if (_colorHistory.Count <= offset)
		{
			return false;
		}

		var currentIndex = _colorHistory.Count - offset;
		if (currentIndex <= 0 || currentIndex >= _colorHistory.Count)
		{
			return false;
		}

		currentColor = _colorHistory[currentIndex];
		previousColor = _colorHistory[currentIndex - 1];
		return true;
	}

	private static int DetermineColor(decimal histogram, decimal upper1, decimal upper2, decimal lower1, decimal lower2)
	{
		if (histogram > upper2)
		return 0;
		if (histogram > upper1)
		return 1;
		if (histogram < lower2)
		return 4;
		if (histogram < lower1)
		return 3;
		return 2;
	}

	private decimal GetWeightedVolume(ICandleMessage candle)
	{
		var volume = candle.TotalVolume ?? 0m;
		return VolumeMode == VolumeCalculationMode.Tick
		? (volume <= 0m ? 1m : volume)
		: (volume > 0m ? volume : 1m);
	}

	private static IIndicator CreateSmoother(XrsiSmoothingMethod method, int length)
	{
		var effectiveLength = Math.Max(1, length);
		return method switch
		{
			XrsiSmoothingMethod.Sma => new SimpleMovingAverage { Length = effectiveLength },
			XrsiSmoothingMethod.Ema => new ExponentialMovingAverage { Length = effectiveLength },
			XrsiSmoothingMethod.Smma => new SmoothedMovingAverage { Length = effectiveLength },
			XrsiSmoothingMethod.Lwma => new LinearWeightedMovingAverage { Length = effectiveLength },
			XrsiSmoothingMethod.Jurik => new JurikMovingAverage { Length = effectiveLength },
			_ => new SimpleMovingAverage { Length = effectiveLength },
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_rsi?.Reset();
		_histogramSmoother?.Reset();
		_volumeSmoother?.Reset();
		_colorHistory.Clear();
		base.OnReseted();
	}

	public enum VolumeCalculationMode
	{
		Tick,
		Real
	}

	public enum XrsiSmoothingMethod
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jurik,
		Parabolic,
		T3,
		Vidya,
		Ama
	}
}

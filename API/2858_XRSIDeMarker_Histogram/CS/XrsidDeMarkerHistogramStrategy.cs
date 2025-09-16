using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the XRSI DeMarker histogram indicator.
/// Combines RSI and DeMarker readings, smooths them and trades reversals.
/// </summary>
public class XrsidDeMarkerHistogramStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _allowBuyEntries;
	private readonly StrategyParam<bool> _allowSellEntries;
	private readonly StrategyParam<bool> _closeLongOnShortSignal;
	private readonly StrategyParam<bool> _closeShortOnLongSignal;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<int> _signalBar;

	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _deMaxAverage = null!;
	private SimpleMovingAverage _deMinAverage = null!;
	private LengthIndicator<decimal> _smoother = null!;

	private readonly List<decimal> _indicatorValues = new();
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _entryPrice;

	/// <summary>
	/// Supported applied price types for RSI calculation.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>Use close price.</summary>
		Close,
		/// <summary>Use open price.</summary>
		Open,
		/// <summary>Use high price.</summary>
		High,
		/// <summary>Use low price.</summary>
		Low,
		/// <summary>Use median price ((High + Low) / 2).</summary>
		Median,
		/// <summary>Use typical price ((High + Low + Close) / 3).</summary>
		Typical,
		/// <summary>Use weighted price ((High + Low + 2 * Close) / 4).</summary>
		Weighted
	}

	/// <summary>
	/// Moving average types for smoothing the combined oscillator.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Sma,
		/// <summary>Exponential moving average.</summary>
		Ema,
		/// <summary>Smoothed moving average (RMA).</summary>
		Smma,
		/// <summary>Linear weighted moving average.</summary>
		Lwma,
		/// <summary>Jurik moving average.</summary>
		Jurik,
		/// <summary>Fallback to exponential moving average for unsupported types.</summary>
		Adaptive
	}

	/// <summary>
	/// Default trade volume for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Enable opening long positions on bullish signals.
	/// </summary>
	public bool AllowBuyEntries
	{
		get => _allowBuyEntries.Value;
		set => _allowBuyEntries.Value = value;
	}

	/// <summary>
	/// Enable opening short positions on bearish signals.
	/// </summary>
	public bool AllowSellEntries
	{
		get => _allowSellEntries.Value;
		set => _allowSellEntries.Value = value;
	}

	/// <summary>
	/// Close existing long positions when a short signal appears.
	/// </summary>
	public bool CloseLongOnShortSignal
	{
		get => _closeLongOnShortSignal.Value;
		set => _closeLongOnShortSignal.Value = value;
	}

	/// <summary>
	/// Close existing short positions when a long signal appears.
	/// </summary>
	public bool CloseShortOnLongSignal
	{
		get => _closeShortOnLongSignal.Value;
		set => _closeShortOnLongSignal.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Look-back period for RSI and DeMarker calculations.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mode used by RSI.
	/// </summary>
	public AppliedPrice AppliedPriceSelection
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm for the combined oscillator.
	/// </summary>
	public SmoothingMethod SmoothingMethodSelection
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Smoothing period length.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Phase parameter used by advanced smoothing methods.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Number of closed bars to shift when evaluating signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public XrsidDeMarkerHistogramStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Default order volume when opening positions", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
		.SetDisplay("Stop Loss Ticks", "Protective stop distance in price steps", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
		.SetDisplay("Take Profit Ticks", "Profit target distance in price steps", "Risk");

		_allowBuyEntries = Param(nameof(AllowBuyEntries), true)
		.SetDisplay("Allow Long Entries", "Enable opening long trades on bullish signals", "Trading Rules");

		_allowSellEntries = Param(nameof(AllowSellEntries), true)
		.SetDisplay("Allow Short Entries", "Enable opening short trades on bearish signals", "Trading Rules");

		_closeLongOnShortSignal = Param(nameof(CloseLongOnShortSignal), true)
		.SetDisplay("Close Long on Short Signal", "Exit long trades when a bearish reversal appears", "Trading Rules");

		_closeShortOnLongSignal = Param(nameof(CloseShortOnLongSignal), true)
		.SetDisplay("Close Short on Long Signal", "Exit short trades when a bullish reversal appears", "Trading Rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Indicator Period", "Look-back for RSI and DeMarker", "Indicator");

		_appliedPrice = Param(nameof(AppliedPriceSelection), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price type used for RSI calculation", "Indicator");

		_smoothingMethod = Param(nameof(SmoothingMethodSelection), SmoothingMethod.Sma)
		.SetDisplay("Smoothing Method", "Moving average applied to the combined oscillator", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Period of the smoothing moving average", "Indicator");

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
		.SetDisplay("Smoothing Phase", "Phase parameter for advanced smoothing modes", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars back used for signal detection", "Trading Rules");
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

		_indicatorValues.Clear();
		_previousHigh = null;
		_previousLow = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_rsi = new RelativeStrengthIndex { Length = IndicatorPeriod };
		_deMaxAverage = new SimpleMovingAverage { Length = IndicatorPeriod };
		_deMinAverage = new SimpleMovingAverage { Length = IndicatorPeriod };
		_smoother = CreateSmoother(SmoothingMethodSelection, SmoothingLength, SmoothingPhase);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position == 0m)
		_entryPrice = null;

		if (CheckRiskManagement(candle))
		return;

		var price = GetAppliedPrice(candle, AppliedPriceSelection);

		var rsiValue = _rsi.Process(price);
		if (!rsiValue.IsFinal)
		return;

		if (_previousHigh is null || _previousLow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var deMax = Math.Max(candle.HighPrice - _previousHigh.Value, 0m);
		var deMin = Math.Max(_previousLow.Value - candle.LowPrice, 0m);

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		var deMaxValue = _deMaxAverage.Process(deMax);
		var deMinValue = _deMinAverage.Process(deMin);

		if (!deMaxValue.IsFinal || !deMinValue.IsFinal)
		return;

		var deMaxAverage = deMaxValue.ToDecimal();
		var deMinAverage = deMinValue.ToDecimal();
		var denominator = deMaxAverage + deMinAverage;
		var deMarker = denominator <= 0m ? 0m : deMaxAverage / denominator;

		var combined = (rsiValue.ToDecimal() + 100m * deMarker) / 2m;
		var smoothedValue = _smoother.Process(combined);

		if (!smoothedValue.IsFinal)
		return;

		var indicator = smoothedValue.ToDecimal();
		_indicatorValues.Add(indicator);

		var maxCount = Math.Max(5, SignalBar + 5);
		if (_indicatorValues.Count > maxCount)
		_indicatorValues.RemoveRange(0, _indicatorValues.Count - maxCount);

		var shift = Math.Max(0, SignalBar);
		var index0 = _indicatorValues.Count - 1 - shift;
		var index1 = index0 - 1;
		var index2 = index1 - 1;

		if (index2 < 0)
		return;

		var value0 = _indicatorValues[index0];
		var value1 = _indicatorValues[index1];
		var value2 = _indicatorValues[index2];

		var trendUp = value1 < value2;
		var trendDown = value1 > value2;
		var buySignal = trendUp && value0 >= value1;
		var sellSignal = trendDown && value0 <= value1;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (buySignal)
		{
			if (CloseShortOnLongSignal && Position < 0m)
			CloseShort();

			if (AllowBuyEntries && Position <= 0m)
			OpenLong(candle.ClosePrice);
		}
		else if (sellSignal)
		{
			if (CloseLongOnShortSignal && Position > 0m)
			CloseLong();

			if (AllowSellEntries && Position >= 0m)
			OpenShort(candle.ClosePrice);
		}
	}

	private bool CheckRiskManagement(ICandleMessage candle)
	{
		if (_entryPrice is null || Position == 0m)
		return false;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
		step = 1m;

		var stopLossDistance = StopLossTicks > 0 ? StopLossTicks * step : 0m;
		var takeProfitDistance = TakeProfitTicks > 0 ? TakeProfitTicks * step : 0m;
		var entry = _entryPrice.Value;

		if (Position > 0m)
		{
			if (StopLossTicks > 0 && candle.LowPrice <= entry - stopLossDistance)
			{
				CloseLong();
				return true;
			}

			if (TakeProfitTicks > 0 && candle.HighPrice >= entry + takeProfitDistance)
			{
				CloseLong();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (StopLossTicks > 0 && candle.HighPrice >= entry + stopLossDistance)
			{
				CloseShort();
				return true;
			}

			if (TakeProfitTicks > 0 && candle.LowPrice <= entry - takeProfitDistance)
			{
				CloseShort();
				return true;
			}
		}

		return false;
	}

	private void CloseLong()
	{
		if (Position <= 0m)
		return;

		SellMarket(Position);
		_entryPrice = null;
	}

	private void CloseShort()
	{
		if (Position >= 0m)
		return;

		BuyMarket(Math.Abs(Position));
		_entryPrice = null;
	}

	private void OpenLong(decimal price)
	{
		var volume = Math.Max(0m, TradeVolume);
		if (volume <= 0m)
		return;

		var qty = volume + (Position < 0m ? Math.Abs(Position) : 0m);
		if (qty <= 0m)
		return;

		BuyMarket(qty);
		_entryPrice = price;
	}

	private void OpenShort(decimal price)
	{
		var volume = Math.Max(0m, TradeVolume);
		if (volume <= 0m)
		return;

		var qty = volume + (Position > 0m ? Position : 0m);
		if (qty <= 0m)
		return;

		SellMarket(qty);
		_entryPrice = price;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length, int phase)
	{
		length = Math.Max(1, length);

		return method switch
		{
			SmoothingMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Lwma => new WeightedMovingAverage { Length = length },
			SmoothingMethod.Jurik => new JurikMovingAverage { Length = length, Phase = phase },
			_ => new ExponentialMovingAverage { Length = length }
	};
}
}

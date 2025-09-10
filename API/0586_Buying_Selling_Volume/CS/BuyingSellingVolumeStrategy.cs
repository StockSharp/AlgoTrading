using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on buying/selling volume and volatility bands.
/// Enters long when adjusted buying volume dominates and breaks above the band
/// while price is above weekly VWAP. Enters short on opposite conditions.
/// Uses ATR percentage on daily data for dynamic profit and loss targets.
/// </summary>
public class BuyingSellingVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<decimal> _profitTargetLong;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<decimal> _profitTargetShort;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdev;
	private AverageTrueRange _atr;
	private VolumeWeightedMovingAverage _weeklyVwap;

	private decimal _atrPercent;
	private decimal _weeklyVwapValue;
	private bool _longOpened;
	private bool _shortOpened;
	private decimal _tpLong;
	private decimal _slLong;
	private decimal _tpShort;
	private decimal _slShort;

	private readonly DataType _dailyType = TimeSpan.FromDays(1).TimeFrame();
	private readonly DataType _weeklyType = TimeSpan.FromDays(7).TimeFrame();

	/// <summary>
	/// Length for moving average and standard deviation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Profit target multiplier for long trades.
	/// </summary>
	public decimal ProfitTargetLong
	{
		get => _profitTargetLong.Value;
		set => _profitTargetLong.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier for long trades.
	/// </summary>
	public decimal StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Profit target multiplier for short trades.
	/// </summary>
	public decimal ProfitTargetShort
	{
		get => _profitTargetShort.Value;
		set => _profitTargetShort.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier for short trades.
	/// </summary>
	public decimal StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BuyingSellingVolumeStrategy"/>.
	/// </summary>
	public BuyingSellingVolumeStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "Period for volatility calculation", "Volatility")
			.SetCanOptimize(true);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetDisplay("StdDev", "Standard deviation multiplier", "Volatility")
			.SetCanOptimize(true);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long trades", "General");

		_allowShort = Param(nameof(AllowShort), false)
			.SetDisplay("Allow Short", "Enable short trades", "General");

		_profitTargetLong = Param(nameof(ProfitTargetLong), 100m)
			.SetDisplay("Long TP Mult", "Take profit multiplier for long trades", "Risk")
			.SetCanOptimize(true);

		_stopLossLong = Param(nameof(StopLossLong), 1m)
			.SetDisplay("Long SL Mult", "Stop loss multiplier for long trades", "Risk")
			.SetCanOptimize(true);

		_profitTargetShort = Param(nameof(ProfitTargetShort), 100m)
			.SetDisplay("Short TP Mult", "Take profit multiplier for short trades", "Risk")
			.SetCanOptimize(true);

		_stopLossShort = Param(nameof(StopLossShort), 5m)
			.SetDisplay("Short SL Mult", "Stop loss multiplier for short trades", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for main data", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, _dailyType), (Security, _weeklyType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma?.Reset();
		_stdev?.Reset();
		_atr?.Reset();
		_weeklyVwap?.Reset();

		_atrPercent = 0;
		_weeklyVwapValue = 0;
		_longOpened = false;
		_shortOpened = false;
		_tpLong = _slLong = 0;
		_tpShort = _slShort = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = Length };
		_stdev = new StandardDeviation { Length = Length };
		_atr = new AverageTrueRange { Length = 14 };
		_weeklyVwap = new VolumeWeightedMovingAverage();

		var mainSub = SubscribeCandles(CandleType);
		mainSub.ForEach(ProcessCandle).Start();

		var dailySub = SubscribeCandles(_dailyType);
		dailySub.Bind(_atr, ProcessDaily).Start();

		var weeklySub = SubscribeCandles(_weeklyType);
		weeklySub.Bind(_weeklyVwap, ProcessWeekly).Start();
	}

	private void ProcessWeekly(ICandleMessage candle, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_weeklyVwapValue = vwap;
	}

	private void ProcessDaily(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.ClosePrice > 0)
			_atrPercent = atr / candle.ClosePrice * 100m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		if (volume <= 0)
			volume = 1;

		var bv = high == low ? 0 : volume * (close - low) / (high - low);
		var sv = high == low ? 0 : volume * (high - close) / (high - low);

		var tp = bv + sv;
		var bpv = tp == 0 ? 0 : bv / tp * volume;
		var spv = tp == 0 ? 0 : sv / tp * volume;

		var bpcon = bpv > spv ? bpv : -Math.Abs(bpv);
		var spcon = spv > bpv ? spv : -Math.Abs(spv);
		var minus = bpcon + spcon;

		var basis = _sma.Process(minus).ToDecimal();
		var dev = _stdev.Process(minus).ToDecimal() * Multiplier;
		var upper = basis + dev;

		var longSignal = minus > upper && bpcon > spcon && close > _weeklyVwapValue;
		var shortSignal = minus > upper && bpcon < spcon && close < _weeklyVwapValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (AllowLong && longSignal && Position <= 0 && !_longOpened)
		{
			_tpLong = close + close * (_atrPercent * ProfitTargetLong) / 100m;
			_slLong = close - close * (_atrPercent * StopLossLong) / 100m;
			BuyMarket(Volume + Math.Abs(Position));
			_longOpened = true;
			_shortOpened = false;
		}
		else if (AllowShort && shortSignal && Position >= 0 && !_shortOpened)
		{
			_tpShort = close - close * (_atrPercent * ProfitTargetShort) / 100m;
			_slShort = close + close * (_atrPercent * StopLossShort) / 100m;
			SellMarket(Volume + Math.Abs(Position));
			_shortOpened = true;
			_longOpened = false;
		}

		var tpLongTrigger = _longOpened && (candle.ClosePrice > _tpLong || candle.HighPrice > _tpLong);
		var slLongTrigger = _longOpened && (candle.ClosePrice < _slLong || candle.LowPrice < _slLong);
		var longExit = shortSignal || tpLongTrigger || slLongTrigger;

		if (Position > 0 && longExit)
		{
			ClosePosition();
			_longOpened = false;
		}

		var tpShortTrigger = _shortOpened && (candle.ClosePrice < _tpShort || candle.LowPrice < _tpShort);
		var slShortTrigger = _shortOpened && (candle.ClosePrice > _slShort || candle.HighPrice > _slShort);
		var shortExit = longSignal || tpShortTrigger || slShortTrigger;

		if (Position < 0 && shortExit)
		{
			ClosePosition();
			_shortOpened = false;
		}
	}
}

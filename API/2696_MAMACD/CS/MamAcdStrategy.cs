using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MAMACD trend-following strategy converted from the original MetaTrader 5 expert advisor.
/// Combines two low-price LWMA filters, a fast EMA trigger, and a MACD confirmation filter.
/// </summary>
public class MamAcdStrategy : Strategy
{
	private readonly StrategyParam<int> _firstLowMaLength;
	private readonly StrategyParam<int> _secondLowMaLength;
	private readonly StrategyParam<int> _triggerEmaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _firstLowMa = null!;
	private WeightedMovingAverage _secondLowMa = null!;
	private ExponentialMovingAverage _triggerEma = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _previousMacd;
	private bool _readyForLong;
	private bool _readyForShort;
	private decimal _pipSize;

	/// <summary>
	/// Period of the first LWMA calculated on low prices.
	/// </summary>
	public int FirstLowMaLength
	{
		get => _firstLowMaLength.Value;
		set => _firstLowMaLength.Value = value;
	}

	/// <summary>
	/// Period of the second LWMA calculated on low prices.
	/// </summary>
	public int SecondLowMaLength
	{
		get => _secondLowMaLength.Value;
		set => _secondLowMaLength.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA calculated on close prices.
	/// </summary>
	public int TriggerEmaLength
	{
		get => _triggerEmaLength.Value;
		set => _triggerEmaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Set to zero to disable protective stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Set to zero to disable take-profit.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MamAcdStrategy"/> with default parameters.
	/// </summary>
	public MamAcdStrategy()
	{
		_firstLowMaLength = Param(nameof(FirstLowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("LWMA #1", "Length of the first LWMA on lows", "Indicators")
		.SetCanOptimize(true);

		_secondLowMaLength = Param(nameof(SecondLowMaLength), 75)
		.SetGreaterThanZero()
		.SetDisplay("LWMA #2", "Length of the second LWMA on lows", "Indicators")
		.SetCanOptimize(true);

		_triggerEmaLength = Param(nameof(TriggerEmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Trigger EMA", "Length of the EMA on closes", "Indicators")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 15)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length of MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length of MACD", "Indicators")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 15)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 15)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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

		_previousMacd = null;
		_readyForLong = false;
		_readyForShort = false;
		_pipSize = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstLowMa = new WeightedMovingAverage { Length = FirstLowMaLength };
		_secondLowMa = new WeightedMovingAverage { Length = SecondLowMaLength };
		_triggerEma = new ExponentialMovingAverage { Length = TriggerEmaLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = MacdFastLength },
			LongMa = { Length = MacdSlowLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		_pipSize = CalculatePipSize();

		var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : new Unit();
		var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : new Unit();
		StartProtection(takeProfit, stopLoss);

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _firstLowMa, "LWMA Low #1");
			DrawIndicator(priceArea, _secondLowMa, "LWMA Low #2");
			DrawIndicator(priceArea, _triggerEma, "EMA Trigger");
			DrawOwnTrades(priceArea);

			var macdArea = CreateChartArea("MACD");
			if (macdArea != null)
			{
				DrawIndicator(macdArea, _macd, "MACD");
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Feed indicator chain: LWMAs work on low prices, EMA and MACD on closes.
		var firstLowValue = _firstLowMa.Process(candle.LowPrice, candle.OpenTime, true);
		var secondLowValue = _secondLowMa.Process(candle.LowPrice, candle.OpenTime, true);
		var triggerValue = _triggerEma.Process(candle.ClosePrice, candle.OpenTime, true);
		var macdValue = _macd.Process(candle.ClosePrice, candle.OpenTime, true);

		// Wait for all indicators to collect enough history.
		if (!_firstLowMa.IsFormed || !_secondLowMa.IsFormed || !_triggerEma.IsFormed || !_macd.IsFormed)
		{
			if (_macd.IsFormed)
			_previousMacd = macdValue.ToDecimal();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousMacd = macdValue.ToDecimal();
			return;
		}

		var ma1 = firstLowValue.ToDecimal();
		var ma2 = secondLowValue.ToDecimal();
		var ma3 = triggerValue.ToDecimal();
		var macd = macdValue.ToDecimal();

		// Store the first complete MACD observation before evaluating signals.
		if (_previousMacd is null)
		{
			_previousMacd = macd;
			return;
		}

		// Skip calculations when MACD lacks momentum confirmation just like the original EA.
		if (macd == 0m || _previousMacd.Value == 0m)
		{
			_previousMacd = macd;
			return;
		}

		// Track reset flags: EMA must dip below both LWMAs to prepare for a new long, and rise above them for shorts.
		if (ma3 < ma1 && ma3 < ma2)
		_readyForLong = true;

		if (ma3 > ma1 && ma3 > ma2)
		_readyForShort = true;

		var macdImproving = macd > _previousMacd.Value;
		var longSignal = ma3 > ma1 && ma3 > ma2 && _readyForLong && (macd > 0m || macdImproving);
		var shortSignal = ma3 < ma1 && ma3 < ma2 && _readyForShort && (macd < 0m || !macdImproving);

		if (longSignal && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				_readyForLong = false;
			}
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				_readyForShort = false;
			}
		}

		_previousMacd = macd;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		return 1m;

		var decimals = CountDecimalPlaces(step);

		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int CountDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);

		var count = 0;
		while (value != Math.Truncate(value) && count < 10)
		{
			value *= 10m;
			count++;
		}

		return count;
	}
}

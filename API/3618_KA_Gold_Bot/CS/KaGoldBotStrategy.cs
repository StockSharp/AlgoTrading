namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

public class KaGoldBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useRiskPercent;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingTriggerPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _pipValue;

	private ExponentialMovingAverage _emaKeltner;
	private SimpleMovingAverage _rangeAverage;
	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;

	private readonly decimal?[] _emaSeries = new decimal?[3];
	private readonly decimal?[] _rangeSeries = new decimal?[3];
	private readonly decimal?[] _emaShortSeries = new decimal?[3];
	private readonly decimal?[] _emaLongSeries = new decimal?[3];
	private readonly decimal?[] _closeSeries = new decimal?[3];

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private decimal? _entryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;
	private decimal? _longTrail;
	private decimal? _shortTrail;
	private bool _longTrailingStarted;
	private bool _shortTrailingStarted;

	public KaGoldBotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
		_keltnerPeriod = Param(nameof(KeltnerPeriod), 50).SetDisplay("Keltner Period");
		_emaShortPeriod = Param(nameof(EmaShortPeriod), 10).SetDisplay("EMA 1 Period");
		_emaLongPeriod = Param(nameof(EmaLongPeriod), 200).SetDisplay("EMA 2 Period");
		_fixedVolume = Param(nameof(FixedVolume), 1m).SetDisplay("Fixed Volume");
		_useRiskPercent = Param(nameof(UseRiskPercent), true).SetDisplay("Use Risk Percent");
		_riskPercent = Param(nameof(RiskPercent), 1m).SetDisplay("Risk Percent", "Percent of equity used per trade", "Money Management");
		_stopLossPips = Param(nameof(StopLossPips), 500m).SetDisplay("Stop Loss (pips)");
		_takeProfitPips = Param(nameof(TakeProfitPips), 500m).SetDisplay("Take Profit (pips)");
		_trailingTriggerPips = Param(nameof(TrailingTriggerPips), 300m).SetDisplay("Trailing Trigger (pips)");
		_trailingStopPips = Param(nameof(TrailingStopPips), 300m).SetDisplay("Trailing Stop (pips)");
		_trailingStepPips = Param(nameof(TrailingStepPips), 100m).SetDisplay("Trailing Step (pips)");
		_useTimeFilter = Param(nameof(UseTimeFilter), true).SetDisplay("Use Time Filter");
		_startHour = Param(nameof(StartHour), 2).SetDisplay("Start Hour");
		_startMinute = Param(nameof(StartMinute), 30).SetDisplay("Start Minute");
		_endHour = Param(nameof(EndHour), 21).SetDisplay("End Hour");
		_endMinute = Param(nameof(EndMinute), 0).SetDisplay("End Minute");
		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 65m).SetDisplay("Max Spread (points)");
		_pipValue = Param(nameof(PipValue), 1m).SetDisplay("Pip Value", "Monetary value of one pip", "Money Management");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	public int EmaShortPeriod
	{
		get => _emaShortPeriod.Value;
		set => _emaShortPeriod.Value = value;
	}

	public int EmaLongPeriod
	{
		get => _emaLongPeriod.Value;
		set => _emaLongPeriod.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public bool UseRiskPercent
	{
		get => _useRiskPercent.Value;
		set => _useRiskPercent.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingTriggerPips
	{
		get => _trailingTriggerPips.Value;
		set => _trailingTriggerPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Array.Clear(_emaSeries, 0, _emaSeries.Length);
		Array.Clear(_rangeSeries, 0, _rangeSeries.Length);
		Array.Clear(_emaShortSeries, 0, _emaShortSeries.Length);
		Array.Clear(_emaLongSeries, 0, _emaLongSeries.Length);
		Array.Clear(_closeSeries, 0, _closeSeries.Length);

		ResetProtection();

		_emaKeltner = new() { Length = KeltnerPeriod };
		_rangeAverage = new() { Length = KeltnerPeriod };
		_emaShort = new() { Length = EmaShortPeriod };
		_emaLong = new() { Length = EmaLongPeriod };

		SubscribeLevel1()
		.Bind(OnLevel1)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateIndicators(candle);
		UpdateProtectionFromCandle(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTimeFilter && !IsWithinTradingSession(candle.CloseTime))
		return;

		if (Position != 0)
		return;

		if (!TryGenerateSignal(out var side))
		return;

		if (MaxSpreadPoints > 0 && !IsSpreadAcceptable())
		return;

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		switch (side)
		{
		case Sides.Buy:
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			SetLongProtection(candle.ClosePrice);
			break;
		case Sides.Sell:
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			SetShortProtection(candle.ClosePrice);
			break;
		}
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
		_bestBidPrice = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _bestBidPrice;
		_bestAskPrice = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _bestAskPrice;

		UpdateTrailingByQuotes();
	}

	private void UpdateIndicators(ICandleMessage candle)
	{
		var emaValue = _emaKeltner.Process(new DecimalIndicatorValue(_emaKeltner, candle.ClosePrice));
		var range = candle.HighPrice - candle.LowPrice;
		var rangeValue = _rangeAverage.Process(new DecimalIndicatorValue(_rangeAverage, range));
		var emaShortValue = _emaShort.Process(new DecimalIndicatorValue(_emaShort, candle.ClosePrice));
		var emaLongValue = _emaLong.Process(new DecimalIndicatorValue(_emaLong, candle.ClosePrice));

		ShiftSeries(_emaSeries, emaValue.IsFinal ? emaValue.GetValue<decimal>() : (decimal?)null);
		ShiftSeries(_rangeSeries, rangeValue.IsFinal ? rangeValue.GetValue<decimal>() : (decimal?)null);
		ShiftSeries(_emaShortSeries, emaShortValue.IsFinal ? emaShortValue.GetValue<decimal>() : (decimal?)null);
		ShiftSeries(_emaLongSeries, emaLongValue.IsFinal ? emaLongValue.GetValue<decimal>() : (decimal?)null);
		ShiftSeries(_closeSeries, candle.ClosePrice);
	}

	private void UpdateProtectionFromCandle(ICandleMessage candle)
	{
		if (_entryPrice is null)
		return;

		var bidReference = _bestBidPrice ?? candle.ClosePrice;
		var askReference = _bestAskPrice ?? candle.ClosePrice;

		UpdateTrailingForLong(bidReference);
		UpdateTrailingForShort(askReference);

		CheckLongExit(candle.LowPrice, candle.HighPrice);
		CheckShortExit(candle.LowPrice, candle.HighPrice);
	}

	private void UpdateTrailingByQuotes()
	{
		if (_entryPrice is null)
		return;

		var bid = _bestBidPrice;
		if (bid.HasValue)
		UpdateTrailingForLong(bid.Value);

		var ask = _bestAskPrice;
		if (ask.HasValue)
		UpdateTrailingForShort(ask.Value);

		if (bid.HasValue)
		CheckLongExit(bid.Value, bid.Value);

		if (ask.HasValue)
		CheckShortExit(ask.Value, ask.Value);
	}

	private void UpdateTrailingForLong(decimal referencePrice)
	{
		if (Position <= 0 || _entryPrice is null)
		return;

		var pip = GetPipSize();
		var triggerDistance = TrailingTriggerPips > 0m ? TrailingTriggerPips * pip : 0m;
		var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * pip : 0m;
		var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * pip : 0m;

		var profit = referencePrice - _entryPrice.Value;
		if (!_longTrailingStarted && triggerDistance > 0m && profit >= triggerDistance)
		_longTrailingStarted = true;

		if (!_longTrailingStarted || trailingDistance <= 0m)
		return;

		if (profit < trailingDistance + trailingStep)
		return;

		var desiredStop = referencePrice - trailingDistance;
		if (_longTrail.HasValue && desiredStop - _longTrail.Value < trailingStep)
		return;

		_longTrail = _longStop.HasValue ? Math.Max(_longStop.Value, desiredStop) : desiredStop;
	}

	private void UpdateTrailingForShort(decimal referencePrice)
	{
		if (Position >= 0 || _entryPrice is null)
		return;

		var pip = GetPipSize();
		var triggerDistance = TrailingTriggerPips > 0m ? TrailingTriggerPips * pip : 0m;
		var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * pip : 0m;
		var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * pip : 0m;

		var profit = _entryPrice.Value - referencePrice;
		if (!_shortTrailingStarted && triggerDistance > 0m && profit >= triggerDistance)
		_shortTrailingStarted = true;

		if (!_shortTrailingStarted || trailingDistance <= 0m)
		return;

		if (profit < trailingDistance + trailingStep)
		return;

		var desiredStop = referencePrice + trailingDistance;
		if (_shortTrail.HasValue && _shortTrail.Value - desiredStop < trailingStep)
		return;

		_shortTrail = _shortStop.HasValue ? Math.Min(_shortStop.Value, desiredStop) : desiredStop;
	}

	private void CheckLongExit(decimal lowPrice, decimal highPrice)
	{
		if (Position <= 0)
		return;

		var stop = GetLongStopLevel();
		if (stop.HasValue && lowPrice <= stop.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}

		if (_longTake.HasValue && highPrice >= _longTake.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}
	}

	private void CheckShortExit(decimal lowPrice, decimal highPrice)
	{
		if (Position >= 0)
		return;

		var stop = GetShortStopLevel();
		if (stop.HasValue && highPrice >= stop.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}

		if (_shortTake.HasValue && lowPrice <= _shortTake.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}
	}

	private decimal? GetLongStopLevel()
	{
		if (_longStop is null)
		return _longTrail;

		return _longTrail.HasValue ? Math.Max(_longStop.Value, _longTrail.Value) : _longStop;
	}

	private decimal? GetShortStopLevel()
	{
		if (_shortStop is null)
		return _shortTrail;

		return _shortTrail.HasValue ? Math.Min(_shortStop.Value, _shortTrail.Value) : _shortStop;
	}

	private bool TryGenerateSignal(out Sides side)
	{
		side = default;

		if (!HasSeriesValue(_emaSeries, 2) || !HasSeriesValue(_rangeSeries, 2) || !HasSeriesValue(_emaShortSeries, 2) || !HasSeriesValue(_emaLongSeries, 1) || !HasSeriesValue(_closeSeries, 1))
		return false;

		var ema1 = _emaSeries[1]!.Value;
		var ema2 = _emaSeries[2]!.Value;
		var range1 = _rangeSeries[1]!.Value;
		var range2 = _rangeSeries[2]!.Value;
		var close1 = _closeSeries[1]!.Value;
		var emaShort1 = _emaShortSeries[1]!.Value;
		var emaShort2 = _emaShortSeries[2]!.Value;
		var emaLong1 = _emaLongSeries[1]!.Value;

		var upper1 = ema1 + range1;
		var lower1 = ema1 - range1;
		var upper2 = ema2 + range2;
		var lower2 = ema2 - range2;

		var entryBuy1 = close1 > upper1;
		var entryBuy2 = close1 > emaLong1;
		var entryBuy3 = emaShort2 < upper2 && emaShort1 > upper1;

		if (entryBuy1 && entryBuy2 && entryBuy3)
		{
			side = Sides.Buy;
			return true;
		}

		var entrySell1 = close1 < lower1;
		var entrySell2 = close1 < emaLong1;
		var entrySell3 = emaShort2 > lower2 && emaShort1 < lower1;

		if (entrySell1 && entrySell2 && entrySell3)
		{
			side = Sides.Sell;
			return true;
		}

		return false;
	}

	private bool HasSeriesValue(decimal?[] series, int shift)
	{
		return shift < series.Length && series[shift].HasValue;
	}

	private bool IsSpreadAcceptable()
	{
		if (!TryGetSpread(out var spreadPoints))
		return false;

		if (spreadPoints > MaxSpreadPoints)
		{
			LogInfo($"Spread {spreadPoints:0.###} exceeds maximum {MaxSpreadPoints} points.");
			return false;
		}

		return true;
	}

	private bool TryGetSpread(out decimal spreadPoints)
	{
		spreadPoints = 0m;

		if (!_bestBidPrice.HasValue || !_bestAskPrice.HasValue)
		return false;

		var step = GetPointValue();
		if (step <= 0m)
		return false;

		spreadPoints = (_bestAskPrice.Value - _bestBidPrice.Value) / step;
		return spreadPoints >= 0m;
	}

	private decimal CalculateVolume()
	{
		var volume = FixedVolume;

		if (UseRiskPercent)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? 0m;
			if (equity > 0m)
			{
				var riskAmount = equity * RiskPercent / 100m;
				var riskPips = StopLossPips > 0m ? StopLossPips : TrailingStopPips;
				if (riskAmount > 0m && riskPips > 0m && PipValue > 0m)
				{
					var volumeByRisk = riskAmount / (riskPips * PipValue);
					if (volumeByRisk > 0m)
					volume = volumeByRisk;
				}
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var minVolume = Security?.MinVolume ?? 0m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (volume <= 0m)
		{
			if (minVolume > 0m)
			volume = minVolume;
			else
			volume = FixedVolume;
		}

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (maxVolume > 0m && maxVolume >= minVolume && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private bool IsWithinTradingSession(DateTimeOffset time)
	{
		var localTime = time.LocalDateTime;
		var startTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, StartHour, StartMinute, 0, localTime.Kind);
		var endTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, EndHour, EndMinute, 0, localTime.Kind);

		if (endTime <= startTime)
		return localTime >= startTime || localTime < endTime.AddDays(1);

		return localTime >= startTime && localTime < endTime;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? 1m;
		return step > 0m ? step : 1m;
	}

	private decimal GetPipSize()
	{
		var step = GetPointValue();
		var decimals = Security?.Decimals ?? 0;
		return decimals % 2 == 1 ? step * 10m : step;
	}

	private void SetLongProtection(decimal entryPrice)
	{
		var pip = GetPipSize();
		_longStop = StopLossPips > 0m ? entryPrice - StopLossPips * pip : null;
		_longTake = TakeProfitPips > 0m ? entryPrice + TakeProfitPips * pip : null;
		_longTrail = null;
		_longTrailingStarted = false;
		_shortStop = null;
		_shortTake = null;
		_shortTrail = null;
		_shortTrailingStarted = false;
	}

	private void SetShortProtection(decimal entryPrice)
	{
		var pip = GetPipSize();
		_shortStop = StopLossPips > 0m ? entryPrice + StopLossPips * pip : null;
		_shortTake = TakeProfitPips > 0m ? entryPrice - TakeProfitPips * pip : null;
		_shortTrail = null;
		_shortTrailingStarted = false;
		_longStop = null;
		_longTake = null;
		_longTrail = null;
		_longTrailingStarted = false;
	}

	private void ResetProtection()
	{
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longTrail = null;
		_shortTrail = null;
		_longTrailingStarted = false;
		_shortTrailingStarted = false;
		_entryPrice = null;
	}

	private static void ShiftSeries(decimal?[] series, decimal? value)
	{
		if (series.Length == 0)
		return;

		for (var i = series.Length - 1; i > 0; i--)
		series[i] = series[i - 1];

		series[0] = value;
	}
}


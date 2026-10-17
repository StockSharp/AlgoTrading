using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// WMA trend confirmation, MACD/signal crossover and CCI filter.
/// Fixed protection uses native actual-fill orders; trailing is evaluated
/// conservatively on completed-candle closes with a configurable step.
/// </summary>
public class EscortTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastWmaPeriod, _slowWmaPeriod, _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<int> _macdFastEma, _macdSlowEma, _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLoss, _takeProfit, _trailingStop, _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _macdFast, _macdSlow, _signal, _previousDifference;
	private decimal? _entry, _favorable, _trailingLevel;
	private decimal _trackedPosition;
	private int _samples;
	private Order _pending;

	public int FastWmaPeriod { get => _fastWmaPeriod.Value; set => _fastWmaPeriod.Value = value; }
	public int SlowWmaPeriod { get => _slowWmaPeriod.Value; set => _slowWmaPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciThreshold { get => _cciThreshold.Value; set => _cciThreshold.Value = value; }
	public int MacdFastEma { get => _macdFastEma.Value; set => _macdFastEma.Value = value; }
	public int MacdSlowEma { get => _macdSlowEma.Value; set => _macdSlowEma.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EscortTrendStrategy()
	{
		_fastWmaPeriod = Param(nameof(FastWmaPeriod), 8).SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted moving average", "Indicator");
		_slowWmaPeriod = Param(nameof(SlowWmaPeriod), 18).SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted moving average", "Indicator");
		_cciPeriod = Param(nameof(CciPeriod), 14).SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "Indicator");
		_cciThreshold = Param(nameof(CciThreshold), 100m).SetNotNegative()
			.SetDisplay("CCI Threshold", "Long and short confirmation level", "Indicator");
		_macdFastEma = Param(nameof(MacdFastEma), 8).SetGreaterThanZero()
			.SetDisplay("MACD Fast EMA", "Fast EMA of MACD", "Indicator");
		_macdSlowEma = Param(nameof(MacdSlowEma), 18).SetGreaterThanZero()
			.SetDisplay("MACD Slow EMA", "Slow EMA of MACD", "Indicator");
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9).SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal-line EMA length", "Indicator");
		_stopLoss = Param(nameof(StopLoss), 55m).SetNotNegative()
			.SetDisplay("Stop Loss", "Absolute distance from filled entry", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 200m).SetNotNegative()
			.SetDisplay("Take Profit", "Absolute distance from filled entry", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 35m).SetNotNegative()
			.SetDisplay("Trailing Stop", "Optional close-based trailing distance", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 3m).SetNotNegative()
			.SetDisplay("Trailing Step", "Minimum favorable increment per trail update", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Signal timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetHistory()
	{
		_macdFast = _macdSlow = _signal = _previousDifference = null;
		_entry = _favorable = _trailingLevel = null;
		_trackedPosition = 0m;
		_samples = 0;
		_pending = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetHistory();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetHistory();
		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			useMarketOrders: true);
		var fastWma = new WeightedMovingAverage { Length = FastWmaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowWmaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		SubscribeCandles(CandleType).Bind(fastWma, slowWma, cci, ProcessCandle).Start();
	}

	protected override void OnNewPosition(Position position)
	{
		base.OnNewPosition(position);
		ObservePosition(position);
	}

	protected override void OnPositionChanged(Position position)
	{
		base.OnPositionChanged(position);
		ObservePosition(position);
	}

	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);
		ObservePosition(position);
	}

	private void ObservePosition(Position position)
	{
		if (position.Security != Security || position.Portfolio != Portfolio)
			return;
		var size = position.CurrentValue ?? 0m;
		if (size == 0m)
		{
			_entry = _favorable = _trailingLevel = null;
			_trackedPosition = 0m;
			return;
		}
		if (position.AveragePrice is not decimal average || average <= 0m)
			return;
		if (_entry == average && _trackedPosition * size > 0m)
		{
			_trackedPosition = size;
			return;
		}
		_entry = _favorable = average;
		_trailingLevel = null;
		_trackedPosition = size;
	}

	private static decimal Ema(decimal? old, decimal value, int period)
		=> old is decimal prior ? prior + 2m / (period + 1m) * (value - prior) : value;

	private void ProcessCandle(ICandleMessage candle, decimal fastWma, decimal slowWma, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		_macdFast = Ema(_macdFast, close, MacdFastEma);
		_macdSlow = Ema(_macdSlow, close, MacdSlowEma);
		var macd = _macdFast.Value - _macdSlow.Value;
		_signal = Ema(_signal, macd, MacdSignalPeriod);
		var difference = macd - _signal.Value;
		_samples++;

		var crossUp = _previousDifference is decimal previous && previous <= 0m && difference > 0m;
		var crossDown = _previousDifference is decimal previousDown && previousDown >= 0m && difference < 0m;
		_previousDifference = difference;
		if (_samples < Math.Max(Math.Max(FastWmaPeriod, SlowWmaPeriod),
			Math.Max(CciPeriod, MacdSlowEma + MacdSignalPeriod)))
			return;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (Position != 0m && TrailingStop > 0m && _entry is decimal entry && _favorable is decimal favorable)
		{
			var longSide = Position > 0m;
			var advance = longSide ? close - favorable : favorable - close;
			if (advance >= TrailingStep)
			{
				_favorable = close;
				favorable = close;
			}
			if (_trailingLevel is null && (longSide ? close - entry : entry - close) >= TrailingStop)
				_trailingLevel = longSide ? close - TrailingStop : close + TrailingStop;
			else if (_trailingLevel is not null && advance >= TrailingStep)
			{
				var candidate = longSide ? close - TrailingStop : close + TrailingStop;
				_trailingLevel = longSide
					? Math.Max(_trailingLevel.Value, candidate)
					: Math.Min(_trailingLevel.Value, candidate);
			}
			if (_trailingLevel is decimal trail && (longSide ? close <= trail : close >= trail))
			{
				_pending = longSide ? SellMarket(Position) : BuyMarket(-Position);
				return;
			}
		}

		var buy = fastWma > slowWma && crossUp && cci > CciThreshold;
		var sell = fastWma < slowWma && crossDown && cci < -CciThreshold;
		if (buy)
		{
			if (Position < 0m)
				_pending = BuyMarket(-Position + Volume);
			else if (Position == 0m)
				_pending = BuyMarket(Volume);
		}
		else if (sell)
		{
			if (Position > 0m)
				_pending = SellMarket(Position + Volume);
			else if (Position == 0m)
				_pending = SellMarket(Volume);
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that demonstrates multiple trailing stop techniques including ATR,
/// Parabolic SAR, moving average, percentage profit and fixed pips.
/// A simple entry based on candle direction is used only for demonstration.
/// </summary>
public class UniversalTrailingStopHedgeStrategy : Strategy
{
	private readonly StrategyParam<TrailingMode> _mode;
	private readonly StrategyParam<int> _delta;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _percentProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;

	/// <summary>
	/// Trailing stop calculation mode.
	/// </summary>
	public TrailingMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Offset in ticks for pips, MA and Parabolic SAR modes.
	/// </summary>
	public int Delta { get => _delta.Value; set => _delta.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Multiplier for ATR value.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Percentage of profit kept as trailing stop.
	/// </summary>
	public decimal PercentProfit { get => _percentProfit.Value; set => _percentProfit.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref=\"UniversalTrailingStopHedgeStrategy\"/>.
	/// </summary>
	public UniversalTrailingStopHedgeStrategy()
	{
		_mode = Param(nameof(Mode), TrailingMode.Atr)
			.SetDisplay(\"Trailing Mode\", \"Algorithm to move stop loss\", \"General\");

		_delta = Param(nameof(Delta), 10)
			.SetDisplay(\"Delta\", \"Offset in ticks\", \"Risk\");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay(\"ATR Period\", \"ATR calculation period\", \"Indicators\")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetDisplay(\"ATR Multiplier\", \"ATR multiplier for stop distance\", \"Indicators\")
			.SetGreaterThanZero();

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay(\"SAR Step\", \"Parabolic SAR acceleration\", \"Indicators\")
			.SetGreaterThanZero();

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay(\"SAR Maximum\", \"Parabolic SAR maximum acceleration\", \"Indicators\")
			.SetGreaterThanZero();

		_maPeriod = Param(nameof(MaPeriod), 34)
			.SetDisplay(\"MA Period\", \"Moving average period\", \"Indicators\")
			.SetCanOptimize(true);

		_percentProfit = Param(nameof(PercentProfit), 50m)
			.SetDisplay(\"Percent Profit\", \"Percent of profit to trail\", \"Risk\")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay(\"Candle Type\", \"Timeframe for calculations\", \"General\");
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
		_entryPrice = 0;
		_trailingStop = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		switch (Mode)
		{
			case TrailingMode.Atr:
				var atr = new AverageTrueRange { Length = AtrPeriod };
				subscription.Bind(atr, ProcessAtr).Start();
				break;
			case TrailingMode.ParabolicSar:
				var sar = new ParabolicSar { Acceleration = SarStep, AccelerationMax = SarMax };
				subscription.Bind(sar, ProcessParabolic).Start();
				break;
			case TrailingMode.MovingAverage:
				var ma = new SimpleMovingAverage { Length = MaPeriod };
				subscription.Bind(ma, ProcessMa).Start();
				break;
			case TrailingMode.Percent:
				subscription.Bind(ProcessPercent).Start();
				break;
			default:
				subscription.Bind(ProcessPips).Start();
				break;
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool CheckCandle(ICandleMessage candle)
	{
		return candle.State == CandleStates.Finished && IsFormedAndOnlineAndAllowTrading();
	}

	private void EnterOnCandle(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private decimal TickSize => Security?.PriceStep ?? 1m;

	private void ProcessPips(ICandleMessage candle)
	{
		if (!CheckCandle(candle))
			return;

		var distance = Delta * TickSize;
		ApplyTrailingByDistance(candle, distance);
	}

	private void ProcessAtr(ICandleMessage candle, decimal atr)
	{
		if (!CheckCandle(candle))
			return;

		var distance = atr * AtrMultiplier;
		ApplyTrailingByDistance(candle, distance);
	}

	private void ProcessParabolic(ICandleMessage candle, decimal sar)
	{
		if (!CheckCandle(candle))
			return;

		var offset = Delta * TickSize;
		ApplyTrailingByLevel(candle, sar - offset, sar + offset);
	}

	private void ProcessMa(ICandleMessage candle, decimal ma)
	{
		if (!CheckCandle(candle))
			return;

		var offset = Delta * TickSize;
		ApplyTrailingByLevel(candle, ma - offset, ma + offset);
	}

	private void ProcessPercent(ICandleMessage candle)
	{
		if (!CheckCandle(candle))
			return;

		if (Position == 0)
		{
			EnterOnCandle(candle);
			return;
		}

		if (Position > 0)
		{
			var stop = _entryPrice + (candle.ClosePrice - _entryPrice) * PercentProfit / 100m;
			if (stop > _trailingStop)
				_trailingStop = stop;
			if (candle.LowPrice <= _trailingStop)
			{
				SellMarket(Position);
				_trailingStop = 0;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice - (_entryPrice - candle.ClosePrice) * PercentProfit / 100m;
			if (_trailingStop == 0 || stop < _trailingStop)
				_trailingStop = stop;
			if (candle.HighPrice >= _trailingStop)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStop = 0;
			}
		}
	}

	private void ApplyTrailingByDistance(ICandleMessage candle, decimal distance)
	{
		if (Position == 0)
		{
			EnterOnCandle(candle);
			if (Position > 0)
				_trailingStop = candle.ClosePrice - distance;
			else if (Position < 0)
				_trailingStop = candle.ClosePrice + distance;
			return;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - distance;
			if (newStop > _trailingStop)
				_trailingStop = newStop;
			if (candle.LowPrice <= _trailingStop)
			{
				SellMarket(Position);
				_trailingStop = 0;
			}
		}
		else
		{
			var newStop = candle.ClosePrice + distance;
			if (_trailingStop == 0 || newStop < _trailingStop)
				_trailingStop = newStop;
			if (candle.HighPrice >= _trailingStop)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStop = 0;
			}
		}
	}

	private void ApplyTrailingByLevel(ICandleMessage candle, decimal levelLong, decimal levelShort)
	{
		if (Position == 0)
		{
			EnterOnCandle(candle);
			if (Position > 0)
				_trailingStop = levelLong;
			else if (Position < 0)
				_trailingStop = levelShort;
			return;
		}

		if (Position > 0)
		{
			if (levelLong > _trailingStop)
				_trailingStop = levelLong;
			if (candle.LowPrice <= _trailingStop)
			{
				SellMarket(Position);
				_trailingStop = 0;
			}
		}
		else
		{
			if (_trailingStop == 0 || levelShort < _trailingStop)
				_trailingStop = levelShort;
			if (candle.HighPrice >= _trailingStop)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStop = 0;
			}
		}
	}
}

/// <summary>
/// Supported trailing stop modes.
/// </summary>
public enum TrailingMode
{
	/// <summary>
	/// Fixed distance in pips.
	/// </summary>
	Pips,

	/// <summary>
	/// ATR-based trailing stop.
	/// </summary>
	Atr,

	/// <summary>
	/// Parabolic SAR based trailing stop.
	/// </summary>
	ParabolicSar,

	/// <summary>
	/// Moving average based trailing stop.
	/// </summary>
	MovingAverage,

	/// <summary>
	/// Percentage of profit trailing stop.
	/// </summary>
	Percent
}

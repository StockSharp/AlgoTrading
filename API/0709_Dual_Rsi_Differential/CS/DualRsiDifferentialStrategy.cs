using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual RSI Differential strategy using two RSI periods.
/// Goes long when the difference between long and short RSI is below threshold.
/// Goes short when the difference is above threshold.
/// Optional holding period and take profit / stop loss management.
/// </summary>
public class DualRsiDifferentialStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortRsiPeriod;
	private readonly StrategyParam<int> _longRsiPeriod;
	private readonly StrategyParam<decimal> _rsiDiffLevel;
	private readonly StrategyParam<bool> _useHoldDays;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<TpslCondition> _condition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
private readonly StrategyParam<Sides?> _direction;

	private decimal _entryPrice;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Trade direction options.
	/// </summary>
/// <summary>
/// Take profit and stop loss mode.
/// </summary>
public enum TpslCondition
	{
		None,
		TP,
		SL,
		Both
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Short RSI period.
	/// </summary>
	public int ShortRsiPeriod { get => _shortRsiPeriod.Value; set => _shortRsiPeriod.Value = value; }

	/// <summary>
	/// Long RSI period.
	/// </summary>
	public int LongRsiPeriod { get => _longRsiPeriod.Value; set => _longRsiPeriod.Value = value; }

	/// <summary>
	/// RSI difference threshold.
	/// </summary>
	public decimal RsiDiffLevel { get => _rsiDiffLevel.Value; set => _rsiDiffLevel.Value = value; }

	/// <summary>
	/// Enable exit after specified days.
	/// </summary>
	public bool UseHoldDays { get => _useHoldDays.Value; set => _useHoldDays.Value = value; }

	/// <summary>
	/// Number of days to hold position.
	/// </summary>
	public int HoldDays { get => _holdDays.Value; set => _holdDays.Value = value; }

	/// <summary>
	/// Take profit / stop loss mode.
	/// </summary>
	public TpslCondition Condition { get => _condition.Value; set => _condition.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPerc { get => _takeProfitPerc.Value; set => _takeProfitPerc.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DualRsiDifferentialStrategy"/> class.
	/// </summary>
	public DualRsiDifferentialStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("Short RSI Period", "Period for short RSI", "Parameters");

		_longRsiPeriod = Param(nameof(LongRsiPeriod), 42)
		.SetGreaterThanZero()
		.SetDisplay("Long RSI Period", "Period for long RSI", "Parameters");

		_rsiDiffLevel = Param(nameof(RsiDiffLevel), 5m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Diff Level", "RSI difference threshold", "Parameters");

		_useHoldDays = Param(nameof(UseHoldDays), true)
		.SetDisplay("Use Hold Days", "Enable holding period", "Risk");

		_holdDays = Param(nameof(HoldDays), 5)
		.SetGreaterThanZero()
		.SetDisplay("Hold Days", "Number of days to hold", "Risk");

		_condition = Param(nameof(Condition), TpslCondition.None)
		.SetDisplay("TPSL Condition", "Take profit/stop loss mode", "Risk");

		_takeProfitPerc = Param(nameof(TakeProfitPerc), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPerc = Param(nameof(StopLossPerc), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed side", "General");
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
		_entryPrice = 0m;
		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortRsi = new RelativeStrengthIndex { Length = ShortRsiPeriod };
		var longRsi = new RelativeStrengthIndex { Length = LongRsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(shortRsi, longRsi, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortRsi, decimal longRsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var diff = longRsi - shortRsi;
var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;

		if (allowLong && diff < -RsiDiffLevel && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		_entryTime = candle.OpenTime;
		return;
		}

		if (allowShort && diff > RsiDiffLevel && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		_entryTime = candle.OpenTime;
		return;
		}

		if (Position > 0)
		{
		if (UseHoldDays)
		{
		if ((_entryTime != null && candle.OpenTime - _entryTime >= TimeSpan.FromDays(HoldDays)) || diff > RsiDiffLevel)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}
		}
		else if (diff > RsiDiffLevel)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}

		if (Condition is TpslCondition.TP or TpslCondition.Both)
		{
		var takePrice = _entryPrice * (1 + TakeProfitPerc / 100m);
		if (candle.ClosePrice >= takePrice)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}
		}

		if (Condition is TpslCondition.SL or TpslCondition.Both)
		{
		var stopPrice = _entryPrice * (1 - StopLossPerc / 100m);
		if (candle.ClosePrice <= stopPrice)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		_entryTime = null;
		}
		}
		}
		else if (Position < 0)
		{
		if (UseHoldDays)
		{
		if ((_entryTime != null && candle.OpenTime - _entryTime >= TimeSpan.FromDays(HoldDays)) || diff < -RsiDiffLevel)
		{
		BuyMarket(-Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}
		}
		else if (diff < -RsiDiffLevel)
		{
		BuyMarket(-Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}

		if (Condition is TpslCondition.TP or TpslCondition.Both)
		{
		var takePrice = _entryPrice * (1 - TakeProfitPerc / 100m);
		if (candle.ClosePrice <= takePrice)
		{
		BuyMarket(-Position);
		_entryPrice = 0m;
		_entryTime = null;
		return;
		}
		}

		if (Condition is TpslCondition.SL or TpslCondition.Both)
		{
		var stopPrice = _entryPrice * (1 + StopLossPerc / 100m);
		if (candle.ClosePrice >= stopPrice)
		{
		BuyMarket(-Position);
		_entryPrice = 0m;
		_entryTime = null;
		}
		}
		}
	}
}

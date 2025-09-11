using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FlexiMA x FlexiST strategy using SMA and SuperTrend signals.
/// Enters when price is on the same side of both indicators.
/// Exits on opposite side conditions.
/// </summary>
public class FlexiMaXFlexiStStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stAtrPeriod;
	private readonly StrategyParam<decimal> _stMultiplier;
	private readonly StrategyParam<TradeDirection> _direction;

	/// <summary>
	/// Trade direction.
	/// </summary>
	public enum TradeDirection
	{
		Long,
		Short,
		Both
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int StAtrPeriod { get => _stAtrPeriod.Value; set => _stAtrPeriod.Value = value; }

	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal StMultiplier { get => _stMultiplier.Value; set => _stMultiplier.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FlexiMaXFlexiStStrategy"/> class.
	/// </summary>
	public FlexiMaXFlexiStStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "FlexiMA");

		_stAtrPeriod = Param(nameof(StAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length for SuperTrend", "FlexiST");

		_stMultiplier = Param(nameof(StMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "FlexiST");

		_direction = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed trading direction", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = new SMA { Length = MaPeriod };
		var supertrend = new SuperTrend { Length = StAtrPeriod, Multiplier = StMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maDiff = candle.ClosePrice - maValue;
		var stDiff = candle.ClosePrice - stValue;

		var allowLong = Direction == TradeDirection.Both || Direction == TradeDirection.Long;
		var allowShort = Direction == TradeDirection.Both || Direction == TradeDirection.Short;

		if (allowLong && maDiff > 0 && stDiff > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (allowShort && maDiff < 0 && stDiff < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && maDiff < 0 && stDiff < 0)
			SellMarket();

		if (Position < 0 && maDiff > 0 && stDiff > 0)
			BuyMarket();
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades around earnings dates with fixed quantity.
/// </summary>
public class EarningsTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeSide> _direction;
	private readonly StrategyParam<DateTimeOffset> _entryDate1;
	private readonly StrategyParam<DateTimeOffset> _exitDate1;
	private readonly StrategyParam<DateTimeOffset> _entryDate2;
	private readonly StrategyParam<DateTimeOffset> _exitDate2;
	private readonly StrategyParam<decimal> _quantity;

	/// <summary>
	/// Trade direction.
	/// </summary>
	public enum TradeSide
	{
		Long,
		Short
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Trade direction.
	/// </summary>
	public TradeSide Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// First entry date.
	/// </summary>
	public DateTimeOffset EntryDate1 { get => _entryDate1.Value; set => _entryDate1.Value = value; }

	/// <summary>
	/// First exit date.
	/// </summary>
	public DateTimeOffset ExitDate1 { get => _exitDate1.Value; set => _exitDate1.Value = value; }

	/// <summary>
	/// Second entry date.
	/// </summary>
	public DateTimeOffset EntryDate2 { get => _entryDate2.Value; set => _entryDate2.Value = value; }

	/// <summary>
	/// Second exit date.
	/// </summary>
	public DateTimeOffset ExitDate2 { get => _exitDate2.Value; set => _exitDate2.Value = value; }

	/// <summary>
	/// Trade quantity.
	/// </summary>
	public decimal Quantity { get => _quantity.Value; set => _quantity.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EarningsTradingStrategy"/> class.
	/// </summary>
	public EarningsTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_direction = Param(nameof(Direction), TradeSide.Long)
			.SetDisplay("Direction", "Trade direction", "General");

		_entryDate1 = Param(nameof(EntryDate1), new DateTimeOffset(2025, 3, 13, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Entry Date 1", "First entry date", "Timing");

		_exitDate1 = Param(nameof(ExitDate1), new DateTimeOffset(2025, 3, 17, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Exit Date 1", "First exit date", "Timing");

		_entryDate2 = Param(nameof(EntryDate2), new DateTimeOffset(2025, 6, 12, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Entry Date 2", "Second entry date", "Timing");

		_exitDate2 = Param(nameof(ExitDate2), new DateTimeOffset(2025, 6, 16, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Exit Date 2", "Second exit date", "Timing");

		_quantity = Param(nameof(Quantity), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Quantity", "Order size", "General");
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

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		var isEntry = date == EntryDate1.Date || date == EntryDate2.Date;
		var isExit = date == ExitDate1.Date || date == ExitDate2.Date;

		if (Direction == TradeSide.Long)
		{
			if (isEntry && Position <= 0)
			{
				var qty = Quantity + Math.Abs(Position);
				BuyMarket(qty);
			}
			else if (isExit && Position > 0)
			{
				SellMarket(Position);
			}
		}
		else
		{
			if (isEntry && Position >= 0)
			{
				var qty = Quantity + Math.Abs(Position);
				SellMarket(qty);
			}
			else if (isExit && Position < 0)
			{
				BuyMarket(-Position);
			}
		}
	}
}

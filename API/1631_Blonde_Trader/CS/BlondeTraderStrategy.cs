using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blonde Trader grid strategy converted from MQL.
/// Opens a position when price moves away from recent extremes
/// and places a grid of pending orders.
/// </summary>
public class BlondeTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _periodX;
	private readonly StrategyParam<int> _limit;
	private readonly StrategyParam<int> _grid;
	private readonly StrategyParam<decimal> _amount;
	private readonly StrategyParam<int> _lockDown;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _entryPrice;
	private bool _stopPlaced;

	/// <summary>
	/// Lookback period for highest high and lowest low.
	/// </summary>
	public int PeriodX { get => _periodX.Value; set => _periodX.Value = value; }

	/// <summary>
	/// Minimum distance in ticks from current price to extreme.
	/// </summary>
	public int Limit { get => _limit.Value; set => _limit.Value = value; }

	/// <summary>
	/// Grid step in ticks between pending orders.
	/// </summary>
	public int Grid { get => _grid.Value; set => _grid.Value = value; }

	/// <summary>
	/// Profit target in account currency.
	/// </summary>
	public decimal Amount { get => _amount.Value; set => _amount.Value = value; }

	/// <summary>
	/// Distance in ticks to move stop loss to breakeven.
	/// </summary>
	public int LockDown { get => _lockDown.Value; set => _lockDown.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="BlondeTraderStrategy"/>.
	/// </summary>
	public BlondeTraderStrategy()
	{
		_periodX = Param(nameof(PeriodX), 1)
			.SetDisplay("Period X", "Lookback for extremes", "General");

		_limit = Param(nameof(Limit), 20)
			.SetDisplay("Limit", "Distance from extreme in ticks", "General");

		_grid = Param(nameof(Grid), 20)
			.SetDisplay("Grid", "Grid step in ticks", "General");

		_amount = Param(nameof(Amount), 2m)
			.SetDisplay("Amount", "Profit target in currency", "General");

		_lockDown = Param(nameof(LockDown), 0)
			.SetDisplay("LockDown", "Ticks to move stop to breakeven", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = PeriodX };
		_lowest = new Lowest { Length = PeriodX };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;
		var limitDistance = Limit * step;
		var gridDistance = Grid * step;
		var price = candle.ClosePrice;

		if (ActiveOrders.Count == 0 && Position == 0)
		{
			if (highValue - price > limitDistance)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPlaced = false;

				for (var i = 1; i < 5; i++)
					BuyLimit(price - i * gridDistance, Volume * (decimal)Math.Pow(2, i));
			}
			else if (price - lowValue > limitDistance)
			{
				SellMarket();
				_entryPrice = price;
				_stopPlaced = false;

				for (var j = 1; j < 5; j++)
					SellLimit(price + j * gridDistance, Volume * (decimal)Math.Pow(2, j));
			}
		}
		else
		{
			if (PnL >= Amount)
				CloseAll();

			if (LockDown > 0 && !_stopPlaced && Position != 0)
			{
				if (Position > 0 && price - _entryPrice >= LockDown * step)
				{
					SellStop(_entryPrice + step, Position);
					_stopPlaced = true;
				}
				else if (Position < 0 && _entryPrice - price >= LockDown * step)
				{
					BuyStop(_entryPrice - step, -Position);
					_stopPlaced = true;
				}
			}
		}
	}

	private void CloseAll()
	{
		CancelActiveOrders();
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}

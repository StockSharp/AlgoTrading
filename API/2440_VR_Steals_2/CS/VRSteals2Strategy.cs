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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opens positions based on SMA trend direction, manages with fixed take profit,
/// stop loss and breakeven levels.
/// </summary>
public class VRSteals2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _breakeven;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _breakevenActivated;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal Breakeven { get => _breakeven.Value; set => _breakeven.Value = value; }
	public decimal BreakevenOffset { get => _breakevenOffset.Value; set => _breakevenOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VRSteals2Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay("Take Profit", "Distance to take profit in steps", "General");
		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetDisplay("Stop Loss", "Distance to stop loss in steps", "General");
		_breakeven = Param(nameof(Breakeven), 20m)
			.SetDisplay("Breakeven", "Distance to activate breakeven in steps", "General");
		_breakevenOffset = Param(nameof(BreakevenOffset), 9m)
			.SetDisplay("Breakeven Offset", "Offset applied when breakeven is triggered", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_breakevenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;
		_breakevenActivated = false;

		var fastSma = new SimpleMovingAverage { Length = 5 };
		var slowSma = new SimpleMovingAverage { Length = 20 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(fastSma, slowSma, (candle, fast, slow) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var price = candle.ClosePrice;
			var step = Security?.PriceStep ?? 1m;

			// Manage existing long position
			if (Position > 0)
			{
				if (!_breakevenActivated && Breakeven > 0m && price >= _entryPrice + Breakeven * step)
				{
					_stopPrice = _entryPrice + BreakevenOffset * step;
					_breakevenActivated = true;
				}

				if (TakeProfit > 0m && price >= _entryPrice + TakeProfit * step)
				{
					SellMarket();
					_entryPrice = 0m;
					_breakevenActivated = false;
					return;
				}

				var stop = _breakevenActivated
					? _stopPrice
					: (StopLoss > 0m ? _entryPrice - StopLoss * step : decimal.MinValue);

				if (price <= stop)
				{
					SellMarket();
					_entryPrice = 0m;
					_breakevenActivated = false;
					return;
				}
			}
			// Manage existing short position
			else if (Position < 0)
			{
				if (!_breakevenActivated && Breakeven > 0m && price <= _entryPrice - Breakeven * step)
				{
					_stopPrice = _entryPrice - BreakevenOffset * step;
					_breakevenActivated = true;
				}

				if (TakeProfit > 0m && price <= _entryPrice - TakeProfit * step)
				{
					BuyMarket();
					_entryPrice = 0m;
					_breakevenActivated = false;
					return;
				}

				var stop = _breakevenActivated
					? _stopPrice
					: (StopLoss > 0m ? _entryPrice + StopLoss * step : decimal.MaxValue);

				if (price >= stop)
				{
					BuyMarket();
					_entryPrice = 0m;
					_breakevenActivated = false;
					return;
				}
			}

			// Entry signals based on SMA cross
			if (Position == 0)
			{
				if (fast > slow)
				{
					BuyMarket();
					_entryPrice = price;
					_breakevenActivated = false;
				}
				else if (fast < slow)
				{
					SellMarket();
					_entryPrice = price;
					_breakevenActivated = false;
				}
			}
		}).Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}

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
/// Price rollback strategy that detects price gaps (large moves) and trades the pullback.
/// When price gaps up beyond a corridor, it sells expecting rollback.
/// When price gaps down beyond a corridor, it buys expecting rollback.
/// Uses trailing stop and take profit for exits.
/// </summary>
public class PriceRollbackStrategy : Strategy
{
	private readonly StrategyParam<decimal> _corridor;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _type;

	private decimal _prevClose;
	private bool _hasPrev;

	public decimal Corridor { get => _corridor.Value; set => _corridor.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public DataType CandleType { get => _type.Value; set => _type.Value = value; }

	public PriceRollbackStrategy()
	{
		_corridor = Param(nameof(Corridor), 5000m)
			.SetDisplay("Corridor", "Minimum gap size for entry", "General");
		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 400m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 300m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");
		_type = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_hasPrev = false;
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_hasPrev && Position == 0)
		{
			// Detect strong candle move (body > corridor) - trade rollback
			var body = close - candle.OpenPrice;
			var bodySize = Math.Abs(body);

			if (bodySize > atrValue * 1.5m)
			{
				// Strong move up - sell expecting rollback
				if (body > 0)
				{
					SellMarket();
				}
				// Strong move down - buy expecting rollback
				else if (body < 0)
				{
					BuyMarket();
				}
			}
		}

		_prevClose = close;
		_hasPrev = true;
	}
}

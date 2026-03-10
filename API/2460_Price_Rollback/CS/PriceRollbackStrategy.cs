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
	private decimal _entryPrice;
	private decimal _trailPrice;
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
		_type = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_entryPrice = 0m;
		_trailPrice = 0m;
		_hasPrev = false;
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_entryPrice = 0;
		_trailPrice = 0;

		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (_hasPrev)
		{
			// Check gap between previous close and current open
			var gap = open - _prevClose;

			if (Position == 0)
			{
				// Gap up beyond corridor - sell expecting rollback
				if (gap > Corridor)
				{
					SellMarket();
					_entryPrice = close;
					_trailPrice = close;
				}
				// Gap down beyond corridor - buy expecting rollback
				else if (gap < -Corridor)
				{
					BuyMarket();
					_entryPrice = close;
					_trailPrice = close;
				}
			}
			else if (Position > 0)
			{
				// Update trailing stop for long
				if (close > _trailPrice)
					_trailPrice = close;

				var trailStop = _trailPrice - TrailingStop;
				var stopPrice = _entryPrice - StopLoss;
				var takePrice = _entryPrice + TakeProfit;

				if (close <= Math.Max(trailStop, stopPrice) || close >= takePrice)
				{
					SellMarket(Math.Abs(Position));
					_entryPrice = 0;
					_trailPrice = 0;
				}
			}
			else if (Position < 0)
			{
				// Update trailing stop for short
				if (close < _trailPrice)
					_trailPrice = close;

				var trailStop = _trailPrice + TrailingStop;
				var stopPrice = _entryPrice + StopLoss;
				var takePrice = _entryPrice - TakeProfit;

				if (close >= Math.Min(trailStop, stopPrice) || close <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = 0;
					_trailPrice = 0;
				}
			}
		}

		_prevClose = close;
		_hasPrev = true;
	}
}

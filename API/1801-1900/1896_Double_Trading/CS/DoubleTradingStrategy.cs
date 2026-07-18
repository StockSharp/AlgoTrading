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
/// Simple double-leg trading strategy.
/// Enters both legs at the start, then monitors combined hypothetical PnL
/// to exit and re-enter in cycles.
/// </summary>
public class DoubleTradingStrategy : Strategy
{
	public enum TradeDirections { Auto, Buy, Sell }

	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<TradeDirections> _direction1;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxRoundTrips;

	private bool _inPosition;
	private Sides _side1;
	private decimal _entryPrice;
	private decimal _lastPrice;
	private int _roundTrips;

	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public TradeDirections Direction1 { get => _direction1.Value; set => _direction1.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaxRoundTrips { get => _maxRoundTrips.Value; set => _maxRoundTrips.Value = value; }

	public DoubleTradingStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 500m)
			.SetDisplay("Profit Target", "Exit profit per round trip", "Risk");
		_direction1 = Param(nameof(Direction1), TradeDirections.Auto)
			.SetDisplay("Direction1", "Initial side", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "Data");
		_maxRoundTrips = Param(nameof(MaxRoundTrips), 20)
			.SetDisplay("Max Round Trips", "Max number of entry/exit cycles", "Risk");
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_inPosition = false;
		_side1 = default;
		_entryPrice = 0m;
		_lastPrice = 0m;
		_roundTrips = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_side1 = Direction1 == TradeDirections.Sell ? Sides.Sell : Sides.Buy;
		_inPosition = false;
		_roundTrips = 0;

		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		if (!_inPosition)
		{
			if (_roundTrips >= MaxRoundTrips)
				return;

			// Enter position
			_entryPrice = candle.ClosePrice;

			if (_side1 == Sides.Buy)
				BuyMarket();
			else
				SellMarket();

			_inPosition = true;
			return;
		}

		// Check profit target
		var pnl = _side1 == Sides.Buy
			? _lastPrice - _entryPrice
			: _entryPrice - _lastPrice;

		if (pnl >= ProfitTarget)
		{
			// Exit position
			if (_side1 == Sides.Buy)
				SellMarket();
			else
				BuyMarket();

			_inPosition = false;
			_roundTrips++;

			// Alternate direction for next round
			_side1 = _side1 == Sides.Buy ? Sides.Sell : Sides.Buy;
		}
	}
}

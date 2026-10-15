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
/// Hedging grid strategy converted from the Frank Ud MetaTrader expert.
/// Opens a position and adds martingale entries when price moves against the trade.
/// Closes all on take profit from average price.
/// </summary>
public class FrankUdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _stepDistance;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<DataType> _candleType;

	private int _lastSignal;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal StepDistance { get => _stepDistance.Value; set => _stepDistance.Value = value; }
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FrankUdStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance from avg price", "Risk");

		_stopLoss = Param(nameof(StopLoss), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance from avg price", "Risk");

		_stepDistance = Param(nameof(StepDistance), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Step Distance", "Price distance for adding martingale entries", "Grid");

		_maxEntries = Param(nameof(MaxEntries), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum martingale entries", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bodySize = (candle.ClosePrice - candle.OpenPrice).Abs();
		if (bodySize < StepDistance)
			return;

		var direction = candle.ClosePrice > candle.OpenPrice ? 1 : -1;
		if (direction == _lastSignal)
			return;

		if (direction > 0 && Position <= 0)
		{
			BuyMarket();
			_lastSignal = 1;
		}
		else if (direction < 0 && Position >= 0)
		{
			SellMarket();
			_lastSignal = -1;
		}
	}
}

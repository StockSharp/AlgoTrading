using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Charles 1.3.7 breakout strategy using symmetric price levels.
/// </summary>
public class Charles137Strategy : Strategy
{
	private readonly StrategyParam<decimal> _anchor;
	private readonly StrategyParam<decimal> _trailingProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _buyLevel;
	private decimal _sellLevel;
	private bool _levelsSet;

	public decimal Anchor { get => _anchor.Value; set => _anchor.Value = value; }
	public decimal TrailingProfit { get => _trailingProfit.Value; set => _trailingProfit.Value = value; }
	public decimal StopLossVal { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Charles137Strategy()
	{
		_anchor = Param(nameof(Anchor), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Anchor", "Distance for breakout levels", "General");

		_trailingProfit = Param(nameof(TrailingProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Profit", "Profit target distance", "General");

		_stopLoss = Param(nameof(StopLossVal), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_buyLevel = 0;
		_sellLevel = 0;
		_levelsSet = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (!_levelsSet)
			{
				_buyLevel = price + Anchor;
				_sellLevel = price - Anchor;
				_levelsSet = true;
				return;
			}

			if (price >= _buyLevel)
			{
				BuyMarket();
				_entryPrice = price;
				_levelsSet = false;
			}
			else if (price <= _sellLevel)
			{
				SellMarket();
				_entryPrice = price;
				_levelsSet = false;
			}
			else
			{
				_buyLevel = price + Anchor;
				_sellLevel = price - Anchor;
			}
		}
		else if (Position > 0)
		{
			var profit = price - _entryPrice;
			if (profit >= TrailingProfit || profit <= -StopLossVal)
			{
				SellMarket();
				_levelsSet = false;
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - price;
			if (profit >= TrailingProfit || profit <= -StopLossVal)
			{
				BuyMarket();
				_levelsSet = false;
			}
		}
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R extreme cross strategy.
/// Buys when WPR crosses above oversold level, sells when crossing below overbought level.
/// </summary>
public class ForexFraus4ForM1sStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _wasOversold;
	private bool _wasOverbought;

	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public decimal SellThreshold { get => _sellThreshold.Value; set => _sellThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ForexFraus4ForM1sStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), -90m)
			.SetDisplay("Buy Threshold", "Level crossing up triggers buy", "Trading");

		_sellThreshold = Param(nameof(SellThreshold), -10m)
			.SetDisplay("Sell Threshold", "Level crossing down triggers sell", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_wasOversold = default;
		_wasOverbought = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_wasOversold = false;
		_wasOverbought = false;

		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(wpr, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!wprValue.IsFormed)
			return;

		var wpr = wprValue.ToDecimal();

		// Track oversold/overbought states
		if (wpr < BuyThreshold)
			_wasOversold = true;

		if (wpr > SellThreshold)
			_wasOverbought = true;

		// Buy signal: was oversold and now crossed above threshold
		if (_wasOversold && wpr > BuyThreshold && Position <= 0)
		{
			_wasOversold = false;
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell signal: was overbought and now crossed below threshold
		else if (_wasOverbought && wpr < SellThreshold && Position >= 0)
		{
			_wasOverbought = false;
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}

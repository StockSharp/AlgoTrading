using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based expert advisor strategy replicating classic oversold/overbought rules.
/// Opens trades on level cross with stop loss and take profit.
/// </summary>
public class RsiEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrevRsi;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiEaStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicator");
		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "RSI oversold threshold", "Indicator");
		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "RSI overbought threshold", "Indicator");
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
		_prevRsi = default;
		_hasPrevRsi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		StartProtection(
			new Unit(StopLoss, UnitTypes.Absolute),
			new Unit(TakeProfit, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			_hasPrevRsi = true;
			return;
		}

		if (!_hasPrevRsi)
		{
			_prevRsi = rsi;
			_hasPrevRsi = true;
			return;
		}

		// RSI crosses above buy level (oversold recovery) - buy
		var buyCross = rsi > BuyLevel && _prevRsi <= BuyLevel;
		// RSI crosses below sell level (overbought drop) - sell
		var sellCross = rsi < SellLevel && _prevRsi >= SellLevel;

		if (buyCross && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (sellCross && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevRsi = rsi;
	}
}

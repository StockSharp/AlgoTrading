using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based scalping strategy converted from MetaTrader "scalpen_rsi.mq4".
/// </summary>
public class ScalpRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyMovement;
	private readonly StrategyParam<int> _buyPeriod;
	private readonly StrategyParam<decimal> _buyBreakdown;
	private readonly StrategyParam<decimal> _buyRsiValue;

	private readonly StrategyParam<decimal> _sellMovement;
	private readonly StrategyParam<int> _sellPeriod;
	private readonly StrategyParam<decimal> _sellBreakdown;
	private readonly StrategyParam<decimal> _sellRsiValue;

	private readonly StrategyParam<int> _buyStopLoss;
	private readonly StrategyParam<int> _buyTakeProfit;
	private readonly StrategyParam<int> _sellStopLoss;
	private readonly StrategyParam<int> _sellTakeProfit;

	private readonly StrategyParam<int> _buyMaLength;
	private readonly StrategyParam<int> _sellMaLength;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<int> _tradeDelaySeconds;
	private readonly StrategyParam<int> _maxOpenTrades;

	private readonly List<decimal> _buyRsiValues = new();
	private readonly List<decimal> _sellRsiValues = new();

	private DateTimeOffset _lastTradeTime;
	private decimal _entryPrice;
	private int _openTrades;

	public decimal BuyMovement { get => _buyMovement.Value; set => _buyMovement.Value = value; }
	public int BuyPeriod { get => _buyPeriod.Value; set => _buyPeriod.Value = value; }
	public decimal BuyBreakdown { get => _buyBreakdown.Value; set => _buyBreakdown.Value = value; }
	public decimal BuyRsiValue { get => _buyRsiValue.Value; set => _buyRsiValue.Value = value; }

	public decimal SellMovement { get => _sellMovement.Value; set => _sellMovement.Value = value; }
	public int SellPeriod { get => _sellPeriod.Value; set => _sellPeriod.Value = value; }
	public decimal SellBreakdown { get => _sellBreakdown.Value; set => _sellBreakdown.Value = value; }
	public decimal SellRsiValue { get => _sellRsiValue.Value; set => _sellRsiValue.Value = value; }

	public int BuyStopLoss { get => _buyStopLoss.Value; set => _buyStopLoss.Value = value; }
	public int BuyTakeProfit { get => _buyTakeProfit.Value; set => _buyTakeProfit.Value = value; }
	public int SellStopLoss { get => _sellStopLoss.Value; set => _sellStopLoss.Value = value; }
	public int SellTakeProfit { get => _sellTakeProfit.Value; set => _sellTakeProfit.Value = value; }

	public int BuyMaLength { get => _buyMaLength.Value; set => _buyMaLength.Value = value; }
	public int SellMaLength { get => _sellMaLength.Value; set => _sellMaLength.Value = value; }
	public bool EnableBuy { get => _enableBuy.Value; set => _enableBuy.Value = value; }
	public bool EnableSell { get => _enableSell.Value; set => _enableSell.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public int TradeDelaySeconds { get => _tradeDelaySeconds.Value; set => _tradeDelaySeconds.Value = value; }
	public int MaxOpenTrades { get => _maxOpenTrades.Value; set => _maxOpenTrades.Value = value; }

	public ScalpRsiStrategy()
	{
		_buyMovement = Param(nameof(BuyMovement), 10m)
			.SetDisplay("Buy Movement", "RSI drop vs earlier period", "Buy");
		_buyPeriod = Param(nameof(BuyPeriod), 2)
			.SetDisplay("Buy Period", "Bars back for comparison", "Buy");
		_buyBreakdown = Param(nameof(BuyBreakdown), 5m)
			.SetDisplay("Buy Breakdown", "RSI drop vs previous bar", "Buy");
		_buyRsiValue = Param(nameof(BuyRsiValue), 30m)
			.SetDisplay("Buy RSI", "RSI value threshold", "Buy");

		_sellMovement = Param(nameof(SellMovement), 0.0040m)
			.SetDisplay("Sell Movement", "RSI rise vs earlier period", "Sell");
		_sellPeriod = Param(nameof(SellPeriod), 2)
			.SetDisplay("Sell Period", "Bars back for comparison", "Sell");
		_sellBreakdown = Param(nameof(SellBreakdown), 0.0030m)
			.SetDisplay("Sell Breakdown", "RSI rise vs previous bar", "Sell");
		_sellRsiValue = Param(nameof(SellRsiValue), 30m)
			.SetDisplay("Sell RSI", "RSI value threshold", "Sell");

		_buyStopLoss = Param(nameof(BuyStopLoss), 60)
			.SetDisplay("Buy Stop Loss", "Ticks for stop loss", "Buy");
		_buyTakeProfit = Param(nameof(BuyTakeProfit), 3)
			.SetDisplay("Buy Take Profit", "Ticks for take profit", "Buy");
		_sellStopLoss = Param(nameof(SellStopLoss), 60)
			.SetDisplay("Sell Stop Loss", "Ticks for stop loss", "Sell");
		_sellTakeProfit = Param(nameof(SellTakeProfit), 3)
			.SetDisplay("Sell Take Profit", "Ticks for take profit", "Sell");

		_buyMaLength = Param(nameof(BuyMaLength), 14)
			.SetDisplay("Buy RSI Length", "RSI period for buy", "Buy");
		_sellMaLength = Param(nameof(SellMaLength), 14)
			.SetDisplay("Sell RSI Length", "RSI period for sell", "Sell");
		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow buy trades", "General");
		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow sell trades", "General");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle", "Candle type", "General");

		_tradeDelaySeconds = Param(nameof(TradeDelaySeconds), 360)
			.SetDisplay("Trade Delay", "Seconds between trades", "General");
		_maxOpenTrades = Param(nameof(MaxOpenTrades), 3)
			.SetDisplay("Max Trades", "Maximum open trades", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var buyRsi = new RSI { Length = BuyMaLength };
		var sellRsi = new RSI { Length = SellMaLength };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(buyRsi, sellRsi, ProcessCandle)
			.Start();
	}

	// Process candle and generate trading signals
	private void ProcessCandle(ICandleMessage candle, decimal buyRsi, decimal sellRsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateList(_buyRsiValues, buyRsi, Math.Max(BuyPeriod, 1) + 1);
		UpdateList(_sellRsiValues, sellRsi, Math.Max(SellPeriod, 1) + 1);

		var step = Security?.PriceStep ?? 1m;
		var now = candle.CloseTime;

		// Check buy conditions
		var buySignal = EnableBuy && _buyRsiValues.Count > BuyPeriod
			&& _buyRsiValues.Count >= 2
			&& _buyRsiValues[^1 - BuyPeriod] - _buyRsiValues[^1] >= BuyMovement
			&& _buyRsiValues[^2] - _buyRsiValues[^1] > BuyBreakdown
			&& _buyRsiValues[^1] < BuyRsiValue;

		// Check sell conditions
		var sellSignal = EnableSell && _sellRsiValues.Count > SellPeriod
			&& _sellRsiValues.Count >= 2
			&& _sellRsiValues[^1] - _sellRsiValues[^1 - SellPeriod] >= SellMovement
			&& _sellRsiValues[^1] - _sellRsiValues[^2] > SellBreakdown
			&& _sellRsiValues[^1] > SellRsiValue;

		var canTrade = (now - _lastTradeTime).TotalSeconds > TradeDelaySeconds && _openTrades < MaxOpenTrades;

		if (buySignal && canTrade)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_lastTradeTime = now;
			_openTrades++;
		}
		else if (sellSignal && canTrade)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_lastTradeTime = now;
			_openTrades++;
		}

		if (Position > 0)
		{
			var stop = _entryPrice - BuyStopLoss * step;
			var take = _entryPrice + BuyTakeProfit * step;
			if (candle.ClosePrice <= stop || candle.ClosePrice >= take)
			{
				SellMarket();
				_openTrades = Math.Max(0, _openTrades - 1);
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + SellStopLoss * step;
			var take = _entryPrice - SellTakeProfit * step;
			if (candle.ClosePrice >= stop || candle.ClosePrice <= take)
			{
				BuyMarket();
				_openTrades = Math.Max(0, _openTrades - 1);
			}
		}

		if (Position == 0)
			_openTrades = 0;
	}

	// Maintain limited history of indicator values
	private static void UpdateList(List<decimal> list, decimal value, int max)
	{
		list.Add(value);
		if (list.Count > max)
			list.RemoveAt(0);
	}
}

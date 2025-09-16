using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Charles 1.3.7 breakout strategy using symmetric stop orders and trailing exits.
/// </summary>
public class Charles137Strategy : Strategy
{
	private readonly StrategyParam<int> _anchor;
	private readonly StrategyParam<decimal> _xFactor;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _buyStopPrice;
	private decimal _sellStopPrice;

	public int Anchor { get => _anchor.Value; set => _anchor.Value = value; }
	public decimal XFactor { get => _xFactor.Value; set => _xFactor.Value = value; }
	public int TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public int TrailingProfit { get => _trailingProfit.Value; set => _trailingProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Charles137Strategy()
	{
		_anchor = Param(nameof(Anchor), 25)
			.SetGreaterThanZero()
			.SetDisplay("Anchor", "Distance in price steps for stop orders", "General");

		_xFactor = Param(nameof(XFactor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Hedging volume multiplier", "General");

		_trailingStop = Param(nameof(TrailingStop), 80)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "General");

		_trailingProfit = Param(nameof(TrailingProfit), 150)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Profit", "Profit target in price steps", "General");

		_stopLoss = Param(nameof(StopLoss), 0)
			.SetDisplay("Stop Loss", "Fixed stop loss in price steps", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position == 0)
		{
			CancelActiveOrders();

			var volume = TradeVolume * XFactor;
			_buyStopPrice = candle.ClosePrice + Anchor * step;
			_sellStopPrice = candle.ClosePrice - Anchor * step;

			BuyStop(volume, _buyStopPrice);
			SellStop(volume, _sellStopPrice);
		}
		else if (Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit >= TrailingProfit * step)
				SellMarket(Position);
			else if (StopLoss > 0 && _entryPrice - candle.ClosePrice >= StopLoss * step)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit >= TrailingProfit * step)
				BuyMarket(-Position);
			else if (StopLoss > 0 && candle.ClosePrice - _entryPrice >= StopLoss * step)
				BuyMarket(-Position);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			CancelActiveOrders();
			_entryPrice = _buyStopPrice;
		}
		else if (Position < 0)
		{
			CancelActiveOrders();
			_entryPrice = _sellStopPrice;
		}
		else
		{
			_entryPrice = 0m;
		}
	}
}

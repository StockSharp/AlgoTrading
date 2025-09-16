using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based expert advisor strategy replicating classic oversold/overbought rules.
/// Opens trades on level cross and optionally closes on opposite signal with stops.
/// </summary>
public class RsiEaStrategy : Strategy
{
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _closeBySignal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private decimal _prevRsi;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool OpenBuy { get => _openBuy.Value; set => _openBuy.Value = value; }

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool OpenSell { get => _openSell.Value; set => _openSell.Value = value; }

	/// <summary>
	/// Close position on opposite RSI cross.
	/// </summary>
	public bool CloseBySignal { get => _closeBySignal.Value; set => _closeBySignal.Value = value; }

	/// <summary>
	/// Stop loss size in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit size in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI level to trigger long entry.
	/// </summary>
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }

	/// <summary>
	/// RSI level to trigger short entry.
	/// </summary>
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="RsiEaStrategy"/>.
	/// </summary>
	public RsiEaStrategy()
	{
		_openBuy = Param(nameof(OpenBuy), true)
			.SetDisplay("Open Buy", "Enable long entries", "General");
		_openSell = Param(nameof(OpenSell), true)
			.SetDisplay("Open Sell", "Enable short entries", "General");
		_closeBySignal = Param(nameof(CloseBySignal), true)
			.SetDisplay("Close By Signal", "Close on opposite RSI cross", "General");
		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Loss in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Profit in price units", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trail distance in price units", "Risk");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicator");
		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "RSI oversold threshold", "Indicator");
		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "RSI overbought threshold", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 1m;
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
		_rsi = null!;
		_prevRsi = 0m;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			return;
		}

		var buyCross = rsi > BuyLevel && _prevRsi < BuyLevel;
		var sellCross = rsi < SellLevel && _prevRsi > SellLevel;

		if (buyCross && OpenBuy && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? -Position : 0m));
			_entryPrice = candle.ClosePrice;
			_highestPrice = candle.ClosePrice;
			_lowestPrice = candle.ClosePrice;
		}
		else if (sellCross && OpenSell && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Position : 0m));
			_entryPrice = candle.ClosePrice;
			_highestPrice = candle.ClosePrice;
			_lowestPrice = candle.ClosePrice;
		}
		else if (CloseBySignal)
		{
			if (Position > 0 && sellCross)
				SellMarket(Position);
			else if (Position < 0 && buyCross)
				BuyMarket(-Position);
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			if (StopLoss > 0m && candle.ClosePrice <= _entryPrice - StopLoss)
				SellMarket(Position);
			else if (TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit)
				SellMarket(Position);
			else if (TrailingStop > 0m)
			{
				var trail = _highestPrice - TrailingStop;
				if (candle.ClosePrice <= trail)
					SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

			if (StopLoss > 0m && candle.ClosePrice >= _entryPrice + StopLoss)
				BuyMarket(-Position);
			else if (TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit)
				BuyMarket(-Position);
			else if (TrailingStop > 0m)
			{
				var trail = _lowestPrice + TrailingStop;
				if (candle.ClosePrice >= trail)
					BuyMarket(-Position);
			}
		}

		_prevRsi = rsi;
	}
}

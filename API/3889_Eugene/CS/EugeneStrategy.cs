using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class EugeneStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _counterBuy;
	private int _counterSell;
	private DateTimeOffset? _lastProcessedCandleTime;

	private ICandleMessage? _previousCandle;
	private ICandleMessage? _prePreviousCandle;

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EugeneStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order size used for market orders", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for price pattern detection", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_counterBuy = 0;
		_counterSell = 0;
		_lastProcessedCandleTime = null;
		_previousCandle = null;
		_prePreviousCandle = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Align the strategy order volume with the configured parameter
		Volume = TradeVolume;

		// Subscribe to the selected candle series and process every finished bar
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Plot candles and own trades when a chart area is available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastProcessedCandleTime != candle.OpenTime)
		{
			_lastProcessedCandleTime = candle.OpenTime;
			_counterBuy = 0;
			_counterSell = 0;
		}

		var prev = _previousCandle;
		var prev2 = _prePreviousCandle;

		if (prev == null || prev2 == null)
		{
			UpdateHistory(candle);
			return;
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (canTrade)
		{
			// Identify inside candles and bird patterns from the original MQL logic
			var isInside = prev.HighPrice <= prev2.HighPrice && prev.LowPrice >= prev2.LowPrice;
			var blackInsider = isInside && prev.ClosePrice <= prev.OpenPrice;
			var whiteInsider = isInside && prev.ClosePrice > prev.OpenPrice;
			var whiteBird = whiteInsider && prev2.ClosePrice > prev2.OpenPrice;
			var blackBird = blackInsider && prev2.ClosePrice < prev2.OpenPrice;

			// Calculate the zigzag levels that determine confirmation areas
			var zigBuy = prev.OpenPrice < prev.ClosePrice
				? prev.ClosePrice - (prev.ClosePrice - prev.OpenPrice) / 3m
				: prev.ClosePrice - (prev.ClosePrice - prev.LowPrice) / 3m;

			var zigSell = prev.OpenPrice > prev.ClosePrice
				? prev.ClosePrice + (prev.OpenPrice - prev.ClosePrice) / 3m
				: prev.ClosePrice + (prev.HighPrice - prev.ClosePrice) / 3m;

			// Confirmations combine price pullbacks with a trading session filter
			var confirmBuy = ((candle.LowPrice <= zigBuy) || candle.OpenTime.Hour >= 8)
				&& !blackBird && !whiteInsider;

			var confirmSell = ((candle.HighPrice >= zigSell) || candle.OpenTime.Hour >= 8)
				&& !whiteBird && !blackInsider;

			var buySignal = candle.HighPrice > prev.HighPrice;
			var sellSignal = candle.LowPrice < prev.LowPrice;

			var additionalBuyFilter = candle.LowPrice > prev.LowPrice && prev.LowPrice < prev2.HighPrice;
			var additionalSellFilter = candle.HighPrice < prev.HighPrice;

			if (buySignal && confirmBuy && additionalBuyFilter && Position < 0)
			{
				// Close an existing short position before considering a new long entry
				BuyMarket(Math.Abs(Position));
			}

			if (sellSignal && confirmSell && additionalSellFilter && Position > 0)
			{
				// Close an existing long position before opening a short trade
				SellMarket(Position);
			}

			if (buySignal && confirmBuy && additionalBuyFilter && Position <= 0 && _counterBuy == 0)
			{
				// Reverse from a short position or open a new long position
				var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
				BuyMarket(volume);
				_counterBuy++;
			}

			if (sellSignal && confirmSell && additionalSellFilter && Position >= 0 && _counterSell == 0)
			{
				// Reverse from a long position or open a new short position
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
				_counterSell++;
			}
		}

		UpdateHistory(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prePreviousCandle = _previousCandle;
		_previousCandle = candle;
	}
}

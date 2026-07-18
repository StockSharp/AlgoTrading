using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TST Pullback Reversal: buys after deep pullback from candle high,
/// sells after rally from candle low. Uses ATR for thresholds.
/// </summary>
public class TstStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _pullbackMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<decimal> _takeMultiplier;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public TstStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_pullbackMultiplier = Param(nameof(PullbackMultiplier), 0.5m)
			.SetDisplay("Pullback Mult", "ATR multiplier for pullback threshold.", "Signals");

		_stopMultiplier = Param(nameof(StopMultiplier), 2.0m)
			.SetDisplay("Stop Mult", "ATR multiplier for stop loss.", "Risk");

		_takeMultiplier = Param(nameof(TakeMultiplier), 1.0m)
			.SetDisplay("Take Mult", "ATR multiplier for take profit.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal PullbackMultiplier
	{
		get => _pullbackMultiplier.Value;
		set => _pullbackMultiplier.Value = value;
	}

	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	public decimal TakeMultiplier
	{
		get => _takeMultiplier.Value;
		set => _takeMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var threshold = atrVal * PullbackMultiplier;
		var stopDist = atrVal * StopMultiplier;
		var takeDist = atrVal * TakeMultiplier;

		// Risk management
		if (Position > 0)
		{
			if (_stopPrice > 0 && close <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				return;
			}
			if (_takePrice > 0 && close >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0 && close >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				return;
			}
			if (_takePrice > 0 && close <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				return;
			}
		}

		// Entry: deep pullback from high = buy reversal
		if (Position == 0)
		{
			if (open > close && high - close > threshold)
			{
				_entryPrice = close;
				_stopPrice = close - stopDist;
				_takePrice = close + takeDist;
				BuyMarket();
			}
			else if (close > open && close - low > threshold)
			{
				_entryPrice = close;
				_stopPrice = close + stopDist;
				_takePrice = close - takeDist;
				SellMarket();
			}
		}
	}
}

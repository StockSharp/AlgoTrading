using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Backbone strategy that alternates between long and short based on retracements from recent extremes.
/// Uses ATR to detect when price has pulled back enough from its high/low to enter a new position.
/// </summary>
public class BackboneBasketStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _retraceMultiplier;

	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _entryPrice;
	private int _lastDirection; // 1 = long, -1 = short, 0 = none

	public BackboneBasketStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis.", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR indicator.", "Indicators");

		_retraceMultiplier = Param(nameof(RetraceMultiplier), 2m)
			.SetDisplay("Retrace Multiplier", "ATR multiplier for retracement threshold.", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal RetraceMultiplier
	{
		get => _retraceMultiplier.Value;
		set => _retraceMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestPrice = 0;
		_lowestPrice = decimal.MaxValue;
		_entryPrice = 0;
		_lastDirection = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var close = candle.ClosePrice;

		// Track extremes
		if (close > _highestPrice)
			_highestPrice = close;
		if (close < _lowestPrice)
			_lowestPrice = close;

		var threshold = atrValue * RetraceMultiplier;

		// Exit existing positions on retracement
		if (Position > 0 && close < _highestPrice - threshold)
		{
			SellMarket();
			_entryPrice = 0;
			_lastDirection = 1;
			_lowestPrice = close;
		}
		else if (Position < 0 && close > _lowestPrice + threshold)
		{
			BuyMarket();
			_entryPrice = 0;
			_lastDirection = -1;
			_highestPrice = close;
		}

		// Entry logic - alternate direction
		if (Position == 0)
		{
			if (_lastDirection != 1 && close < _highestPrice - threshold)
			{
				// Price pulled back from high - sell
				SellMarket();
				_entryPrice = close;
				_lastDirection = -1;
				_lowestPrice = close;
			}
			else if (_lastDirection != -1 && close > _lowestPrice + threshold)
			{
				// Price bounced from low - buy
				BuyMarket();
				_entryPrice = close;
				_lastDirection = 1;
				_highestPrice = close;
			}
		}
	}
}

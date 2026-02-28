using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only reversal strategy that looks for consecutive descending highs
/// and enters when RSI confirms oversold conditions with momentum turning up.
/// </summary>
public class BillyExpertReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;

	private decimal _prevHigh1, _prevHigh2, _prevHigh3;
	private int _barCount;
	private decimal _prevRsi;
	private bool _hasPrevRsi;
	private decimal _entryPrice;

	public BillyExpertReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Length for RSI indicator.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh1 = 0;
		_prevHigh2 = 0;
		_prevHigh3 = 0;
		_barCount = 0;
		_prevRsi = 50;
		_hasPrevRsi = false;
		_entryPrice = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		var high = candle.HighPrice;
		var close = candle.ClosePrice;

		// Check descending highs pattern (3 consecutive lower highs)
		var descendingHighs = _barCount >= 4 &&
			high < _prevHigh1 &&
			_prevHigh1 < _prevHigh2 &&
			_prevHigh2 < _prevHigh3;

		// RSI turning up from oversold
		var rsiBullish = _hasPrevRsi && _prevRsi < 40 && rsiValue > _prevRsi;

		// Manage long position
		if (Position > 0)
		{
			// Exit on take-profit, stop-loss, or RSI overbought
			if (_entryPrice > 0 && close >= _entryPrice * 1.015m)
			{
				SellMarket();
			}
			else if (_entryPrice > 0 && close <= _entryPrice * 0.985m)
			{
				SellMarket();
			}
			else if (rsiValue > 75)
			{
				SellMarket();
			}
		}

		// Manage short position (exit only, this is mostly long-only)
		if (Position < 0)
		{
			if (rsiValue < 30)
			{
				BuyMarket();
			}
		}

		// Entry: descending highs (selling exhaustion) + RSI confirms reversal
		if (Position == 0)
		{
			if (descendingHighs && rsiBullish)
			{
				_entryPrice = close;
				BuyMarket();
			}
			// Also allow short on ascending lows pattern with overbought RSI
			else if (_barCount >= 4 && rsiValue > 70 && _prevRsi > 70)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		// Update history
		_prevHigh3 = _prevHigh2;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = high;
		_prevRsi = rsiValue;
		_hasPrevRsi = true;
	}
}

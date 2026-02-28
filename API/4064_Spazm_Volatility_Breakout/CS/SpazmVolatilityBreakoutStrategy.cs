using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy that tracks swing extremes and reverses
/// when price breaks beyond an ATR-based volatility band.
/// </summary>
public class SpazmVolatilityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;

	private decimal _swingHigh;
	private decimal _swingLow;
	private bool _trendUp;
	private bool _initialized;

	public SpazmVolatilityBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "Period for ATR volatility.", "Indicators");

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetDisplay("Multiplier", "ATR multiplier for breakout threshold.", "Indicators");
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

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_swingHigh = 0;
		_swingLow = decimal.MaxValue;
		_trendUp = true;
		_initialized = false;

		var atr = new AverageTrueRange { Length = AtrLength };

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
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var threshold = atrValue * Multiplier;

		if (!_initialized)
		{
			_swingHigh = high;
			_swingLow = low;
			_initialized = true;
			return;
		}

		if (_trendUp)
		{
			// Track swing high
			if (high > _swingHigh)
				_swingHigh = high;

			// Reversal: price breaks below swing high minus threshold
			if (close < _swingHigh - threshold)
			{
				_trendUp = false;
				_swingLow = low;

				// Enter short on trend reversal
				if (Position > 0)
					SellMarket();
				if (Position == 0)
					SellMarket();
			}
		}
		else
		{
			// Track swing low
			if (low < _swingLow)
				_swingLow = low;

			// Reversal: price breaks above swing low plus threshold
			if (close > _swingLow + threshold)
			{
				_trendUp = true;
				_swingHigh = high;

				// Enter long on trend reversal
				if (Position < 0)
					BuyMarket();
				if (Position == 0)
					BuyMarket();
			}
		}
	}
}

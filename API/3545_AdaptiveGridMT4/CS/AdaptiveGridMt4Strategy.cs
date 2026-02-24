using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive grid strategy using ATR-based breakout levels.
/// Simplified from the "Adaptive Grid Mt4" expert advisor to use market orders.
/// </summary>
public class AdaptiveGridMt4Strategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private decimal? _prevClose;
	private decimal? _prevAtr;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// ATR averaging period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Breakout threshold in ATR multiples.
	/// </summary>
	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to drive calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AdaptiveGridMt4Strategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Breakout Multiplier", "ATR multiplier for breakout threshold", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used to trigger grid recalculation", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevAtr = null;
		_stopPrice = 0;
		_takeProfitPrice = 0;

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || atrValue <= 0)
		{
			_prevClose = candle.ClosePrice;
			_prevAtr = atrValue;
			return;
		}

		// Check protective stops
		if (Position > 0)
		{
			if (_stopPrice > 0 && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
			else if (_takeProfitPrice > 0 && candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0 && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
			else if (_takeProfitPrice > 0 && candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}

		if (_prevClose is not decimal prevClose || _prevAtr is not decimal prevAtr || prevAtr <= 0)
		{
			_prevClose = candle.ClosePrice;
			_prevAtr = atrValue;
			return;
		}

		var threshold = prevAtr * BreakoutMultiplier;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Breakout up
		if (candle.ClosePrice > prevClose + threshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atrValue * 2;
			_takeProfitPrice = candle.ClosePrice + atrValue * 2.5m;
		}
		// Breakout down
		else if (candle.ClosePrice < prevClose - threshold && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atrValue * 2;
			_takeProfitPrice = candle.ClosePrice - atrValue * 2.5m;
		}

		_prevClose = candle.ClosePrice;
		_prevAtr = atrValue;
	}
}

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRTR reversal strategy using ATR-based trailing stop.
/// Maintains a trailing line based on ATR distance from price extremes.
/// Reverses position when price crosses the trailing line.
/// </summary>
public class NrtrReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trailLine;
	private decimal _extreme;
	private int _trend; // 1 = up, -1 = down, 0 = init
	private bool _isInitialized;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NrtrReversalStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for trailing", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing distance", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isInitialized = false;
		_trend = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var offset = atrValue * AtrMultiplier;

		if (!_isInitialized)
		{
			_extreme = close;
			_trailLine = close - offset;
			_trend = 1;
			_isInitialized = true;
			return;
		}

		if (_trend == 1)
		{
			if (close > _extreme)
				_extreme = close;

			_trailLine = Math.Max(_trailLine, _extreme - offset);

			if (close < _trailLine)
			{
				// Switch to downtrend
				_trend = -1;
				_extreme = close;
				_trailLine = close + offset;

				if (Position > 0)
					SellMarket();
				SellMarket();
			}
			else if (Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
		}
		else
		{
			if (close < _extreme)
				_extreme = close;

			_trailLine = Math.Min(_trailLine, _extreme + offset);

			if (close > _trailLine)
			{
				// Switch to uptrend
				_trend = 1;
				_extreme = close;
				_trailLine = close - offset;

				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}
	}
}

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji Trader breakout strategy.
/// Detects doji candles and enters on breakout of doji range.
/// Uses SMA as trend filter for direction.
/// </summary>
public class DojiTraderBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _dojiRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevWasDoji;
	private bool _hasPrev;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public decimal DojiRatio { get => _dojiRatio.Value; set => _dojiRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DojiTraderBreakoutStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "SMA period for trend filter", "Indicators");

		_dojiRatio = Param(nameof(DojiRatio), 0.25m)
			.SetDisplay("Doji Ratio", "Max body/range ratio for doji detection", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_prevWasDoji = false;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isDoji = range > 0 && body / range < DojiRatio;

		if (_hasPrev && _prevWasDoji)
		{
			// Bullish breakout above doji high with SMA confirmation
			if (candle.ClosePrice > _prevHigh && candle.ClosePrice > sma && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			// Bearish breakout below doji low with SMA confirmation
			else if (candle.ClosePrice < _prevLow && candle.ClosePrice < sma && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevWasDoji = isDoji;
		_hasPrev = true;
	}
}

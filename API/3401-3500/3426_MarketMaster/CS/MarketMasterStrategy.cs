namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Market Master strategy: EMA trend with RSI momentum filter.
/// Buys when above EMA and RSI rising, sells when below EMA and RSI falling.
/// </summary>
public class MarketMasterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevRsi;
	private bool _hasPrevRsi;
	private bool _wasBullish;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public MarketMasterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0m;
		_hasPrevRsi = false;
		_wasBullish = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevRsi = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var isBullish = close > emaValue && rsiValue > _prevRsi;

		if (_hasPrevRsi)
		{
			if (isBullish && !_wasBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && close < emaValue && rsiValue < _prevRsi && _wasBullish && Position >= 0)
				SellMarket();
		}

		_prevRsi = rsiValue;
		_hasPrevRsi = true;
		_wasBullish = isBullish;
	}
}

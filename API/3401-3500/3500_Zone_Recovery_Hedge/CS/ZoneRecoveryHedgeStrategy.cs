namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Zone Recovery Hedge strategy: RSI mean reversion with trend filter.
/// Buys when RSI crosses above oversold with close above SMA, sells on overbought cross below SMA.
/// </summary>
public class ZoneRecoveryHedgeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	public ZoneRecoveryHedgeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA trend filter period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevRsi < 30 && rsiValue >= 30 && candle.ClosePrice > smaValue && Position <= 0)
				BuyMarket();
			else if (_prevRsi > 70 && rsiValue <= 70 && candle.ClosePrice < smaValue && Position >= 0)
				SellMarket();
		}

		_prevRsi = rsiValue;
		_hasPrev = true;
	}
}

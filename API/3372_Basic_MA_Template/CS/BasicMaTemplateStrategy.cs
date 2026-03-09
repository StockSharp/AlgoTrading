namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Basic MA Template strategy: trades when price crosses a moving average.
/// Opens long when previous candle crosses above MA, short when crosses below.
/// </summary>
public class BasicMaTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private decimal? _prevOpen;
	private decimal? _prevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	public BasicMaTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_maPeriod = Param(nameof(MaPeriod), 49)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevOpen = null;
		_prevClose = null;
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevOpen.HasValue && _prevClose.HasValue)
		{
			if (_prevOpen.Value > sma && _prevClose.Value < sma && Position >= 0)
				SellMarket();
			else if (_prevOpen.Value < sma && _prevClose.Value > sma && Position <= 0)
				BuyMarket();
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}

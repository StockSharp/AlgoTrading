namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// XRSI DeMarker Histogram strategy (simplified).
/// Uses RSI combined with momentum to detect reversals.
/// Buys on RSI reversal from oversold, sells on reversal from overbought.
/// </summary>
public class XrsidDeMarkerHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal _prevRsi;
	private decimal _prevPrevRsi;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public XrsidDeMarkerHistogramStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI indicator period", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Smoothing period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0m;
		_prevPrevRsi = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_prevPrevRsi = 0;
		_initialized = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, sma, (ICandleMessage candle, decimal rsiValue, decimal smaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!_initialized)
				{
					_prevPrevRsi = rsiValue;
					_prevRsi = rsiValue;
					_initialized = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					_prevPrevRsi = _prevRsi;
					_prevRsi = rsiValue;
					return;
				}

				// Buy on RSI reversal from oversold (V-bottom)
				var buySignal = _prevPrevRsi > _prevRsi && rsiValue >= _prevRsi && _prevRsi < 35m;
				// Sell on RSI reversal from overbought (inverse V)
				var sellSignal = _prevPrevRsi < _prevRsi && rsiValue <= _prevRsi && _prevRsi > 65m;

				if (buySignal && Position <= 0)
				{
					BuyMarket();
				}
				else if (sellSignal && Position >= 0)
				{
					SellMarket();
				}

				_prevPrevRsi = _prevRsi;
				_prevRsi = rsiValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}

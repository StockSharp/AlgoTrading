namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// AO Lightning strategy.
/// Trades based on the Awesome Oscillator momentum slope direction.
/// Buys when AO is rising, sells when AO is falling.
/// </summary>
public class AoLightningStrategy : Strategy
{
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAo;
	private bool _initialized;

	public int AoShortPeriod
	{
		get => _aoShortPeriod.Value;
		set => _aoShortPeriod.Value = value;
	}

	public int AoLongPeriod
	{
		get => _aoLongPeriod.Value;
		set => _aoLongPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AoLightningStrategy()
	{
		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Fast", "Short SMA period for Awesome Oscillator", "Indicators");

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Slow", "Long SMA period for Awesome Oscillator", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = 0;
		_initialized = false;

		var ao = new AwesomeOscillator
		{
			ShortMa = { Length = AoShortPeriod },
			LongMa = { Length = AoLongPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevAo = aoValue;
			_initialized = true;
			return;
		}

		// Buy when AO is rising (bullish momentum)
		if (aoValue > _prevAo && Position <= 0)
		{
			BuyMarket();
		}
		// Sell when AO is falling (bearish momentum)
		else if (aoValue < _prevAo && Position >= 0)
		{
			SellMarket();
		}

		_prevAo = aoValue;
	}
}

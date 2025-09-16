using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Center of Gravity Candle indicator.
/// Opens long position when indicator becomes positive and short when negative.
/// </summary>
public class CenterOfGravityCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCogValue;
	private bool _isInitialized;

	/// <summary>
	/// Main period of the Center of Gravity indicator.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing period of the indicator.
	/// </summary>
	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	/// <summary>
	/// Bar shift used to generate a signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CenterOfGravityCandleStrategy"/>.
	/// </summary>
	public CenterOfGravityCandleStrategy()
	{
		_period = Param(nameof(Period), 10)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Center of Gravity period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_smoothPeriod = Param(nameof(SmoothPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smooth Period", "Smoothing period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Bar shift for signal", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevCogValue = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var cog = new CenterOfGravityOscillator
		{
			Length = Period,
			SmoothLength = SmoothPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(cog, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cogValue)
	{
		// Only process finished candles
		if (candle.State != CandleStates.Finished)
		return;

		// Ensure trading is allowed and connection is online
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			_prevCogValue = cogValue;
			_isInitialized = true;
			return;
		}

		// Detect crossings of the zero line
		if (_prevCogValue <= 0m && cogValue > 0m)
		{
			// Bullish signal
			if (Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevCogValue >= 0m && cogValue < 0m)
		{
			// Bearish signal
			if (Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevCogValue = cogValue;
	}
}

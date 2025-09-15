using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Aroon Oscillator crossing predefined levels.
/// Opens long positions when the oscillator rises above the down level.
/// Opens short positions when the oscillator falls below the up level.
/// </summary>
public class AroonOscillatorSignAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _aroonPeriod;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousValue;
	private bool _isInitialized;

	/// <summary>
	/// Period for the Aroon oscillator.
	/// </summary>
	public int AroonPeriod
	{
		get => _aroonPeriod.Value;
		set => _aroonPeriod.Value = value;
	}

	/// <summary>
	/// Threshold generating sell signal when crossed from above.
	/// </summary>
	public int UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Threshold generating buy signal when crossed from below.
	/// </summary>
	public int DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AroonOscillatorSignAlertStrategy"/> class.
	/// </summary>
	public AroonOscillatorSignAlertStrategy()
	{
		_aroonPeriod = Param(nameof(AroonPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Lookback for Aroon oscillator", "Indicator")
			.SetCanOptimize(true);

		_upLevel = Param(nameof(UpLevel), 50)
			.SetDisplay("Up Level", "Upper threshold for sell signal", "Indicator");

		_downLevel = Param(nameof(DownLevel), -50)
			.SetDisplay("Down Level", "Lower threshold for buy signal", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
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
		_previousValue = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var aroon = new AroonOscillator { Length = AroonPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(aroon, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_previousValue = aroonValue;
			_isInitialized = true;
			return;
		}

		// Cross above the down level triggers a long entry
		if (_previousValue <= DownLevel && aroonValue > DownLevel)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		// Cross below the up level triggers a short entry
		else if (_previousValue >= UpLevel && aroonValue < UpLevel)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_previousValue = aroonValue;
	}
}
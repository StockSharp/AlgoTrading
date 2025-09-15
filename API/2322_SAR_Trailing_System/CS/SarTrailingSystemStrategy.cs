namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that opens random trades and uses Parabolic SAR for trailing exits.
/// </summary>
public class SarTrailingSystemStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _timerInterval;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<decimal> _accelerationStep;
	private readonly StrategyParam<decimal> _accelerationMax;
	private readonly StrategyParam<bool> _useRandomEntry;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();

	private ParabolicSar _parabolicSar;
	private DateTimeOffset _nextTradeTime;
	private bool _isLong;

	/// <summary>
	/// Timer interval for random trade attempts.
	/// </summary>
	public TimeSpan TimerInterval
	{
		get => _timerInterval.Value;
		set => _timerInterval.Value = value;
	}

	/// <summary>
	/// Initial stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal AccelerationStep
	{
		get => _accelerationStep.Value;
		set => _accelerationStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal AccelerationMax
	{
		get => _accelerationMax.Value;
		set => _accelerationMax.Value = value;
	}

	/// <summary>
	/// Enable random entry logic.
	/// </summary>
	public bool UseRandomEntry
	{
		get => _useRandomEntry.Value;
		set => _useRandomEntry.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SarTrailingSystemStrategy"/>.
	/// </summary>
	public SarTrailingSystemStrategy()
	{
		_timerInterval = Param(nameof(TimerInterval), TimeSpan.FromSeconds(300))
			.SetDisplay("Timer Interval", "Time between random trade attempts", "General");

		_stopLossTicks = Param(nameof(StopLossTicks), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Initial stop loss in ticks", "Risk");

		_accelerationStep = Param(nameof(AccelerationStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Indicators");

		_accelerationMax = Param(nameof(AccelerationMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Indicators");

		_useRandomEntry = Param(nameof(UseRandomEntry), true)
			.SetDisplay("Use Random Entry", "Enable random trade entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_parabolicSar = null;
		_nextTradeTime = default;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_parabolicSar = new ParabolicSar
		{
			Acceleration = AccelerationStep,
			AccelerationMax = AccelerationMax
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_parabolicSar, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossTicks, UnitTypes.Step),
			isStopTrailing: false
		);

		_nextTradeTime = time + TimerInterval;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0 && UseRandomEntry && IsFormedAndOnlineAndAllowTrading() && candle.CloseTime >= _nextTradeTime)
		{
			_isLong = _random.NextDouble() >= 0.5;

			if (_isLong)
				BuyMarket();
			else
				SellMarket();

			_nextTradeTime = candle.CloseTime + TimerInterval;
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice <= sarValue)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= sarValue)
				BuyMarket(-Position);
		}
	}
}
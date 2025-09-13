using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy using three timeframes.
/// </summary>
public class ThreeParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _higherTimeframe;
	private readonly StrategyParam<DataType> _middleTimeframe;
	private readonly StrategyParam<DataType> _tradingTimeframe;

	private ParabolicSar _higherSar;
	private ParabolicSar _middleSar;
	private ParabolicSar _tradingSar;

	private bool _higherUp;
	private bool _middleUp;
	private bool? _prevTradingAbove;

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreeParabolicSarStrategy"/> class.
	/// </summary>
	public ThreeParabolicSarStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetDisplay("Acceleration", "Parabolic SAR acceleration", "SAR")
			.SetCanOptimize(true);

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetDisplay("Max Acceleration", "Parabolic SAR maximum acceleration", "SAR")
			.SetCanOptimize(true);

		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Higher Timeframe", "First timeframe", "General");

		_middleTimeframe = Param(nameof(MiddleTimeframe), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Middle Timeframe", "Second timeframe", "General");

		_tradingTimeframe = Param(nameof(TradingTimeframe), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading Timeframe", "Third timeframe", "General");
	}

	/// <summary>
	/// Parabolic SAR acceleration.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// First (highest) timeframe.
	/// </summary>
	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}

	/// <summary>
	/// Second (middle) timeframe.
	/// </summary>
	public DataType MiddleTimeframe
	{
		get => _middleTimeframe.Value;
		set => _middleTimeframe.Value = value;
	}

	/// <summary>
	/// Third timeframe used for trading.
	/// </summary>
	public DataType TradingTimeframe
	{
		get => _tradingTimeframe.Value;
		set => _tradingTimeframe.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, HigherTimeframe);
		yield return (Security, MiddleTimeframe);
		yield return (Security, TradingTimeframe);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_higherSar = null;
		_middleSar = null;
		_tradingSar = null;
		_prevTradingAbove = null;
		_higherUp = false;
		_middleUp = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_higherSar = new ParabolicSar { Acceleration = Acceleration, AccelerationMax = MaxAcceleration };
		_middleSar = new ParabolicSar { Acceleration = Acceleration, AccelerationMax = MaxAcceleration };
		_tradingSar = new ParabolicSar { Acceleration = Acceleration, AccelerationMax = MaxAcceleration };

		var higherSub = SubscribeCandles(HigherTimeframe);
		higherSub.Bind(_higherSar, ProcessHigher).Start();

		var middleSub = SubscribeCandles(MiddleTimeframe);
		middleSub.Bind(_middleSar, ProcessMiddle).Start();

		var tradingSub = SubscribeCandles(TradingTimeframe);
		tradingSub.Bind(_tradingSar, ProcessTrading).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSub);
			DrawIndicator(area, _higherSar);
			DrawIndicator(area, _middleSar);
			DrawIndicator(area, _tradingSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigher(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherUp = candle.ClosePrice > sar;
	}

	private void ProcessMiddle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_middleUp = candle.ClosePrice > sar;
	}

	private void ProcessTrading(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_higherSar.IsFormed || !_middleSar.IsFormed || !_tradingSar.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isAbove = sar > candle.ClosePrice;

		if (_prevTradingAbove != null)
		{
			var crossedDown = _prevTradingAbove == true && !isAbove;
			var crossedUp = _prevTradingAbove == false && isAbove;

			if (crossedDown && _higherUp && _middleUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossedUp && !_higherUp && !_middleUp && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTradingAbove = isAbove;

		if (Position > 0 && (!_higherUp || !_middleUp || isAbove))
			ClosePosition();
		else if (Position < 0 && (_higherUp || _middleUp || !isAbove))
			ClosePosition();
	}
}

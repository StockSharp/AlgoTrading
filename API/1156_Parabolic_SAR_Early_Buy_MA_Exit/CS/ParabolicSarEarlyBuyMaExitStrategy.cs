namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Parabolic SAR strategy with early buy and MA-based exit.
/// </summary>
public class ParabolicSarEarlyBuyMaExitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousSar;
	private bool _previousIsAbove;

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarStart { get => _sarStart.Value; set => _sarStart.Value = value; }

	/// <summary>
	/// Acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarIncrement { get => _sarIncrement.Value; set => _sarIncrement.Value = value; }

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }

	/// <summary>
	/// Period for moving average used in exit.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ParabolicSarEarlyBuyMaExitStrategy"/>.
	/// </summary>
	public ParabolicSarEarlyBuyMaExitStrategy()
	{
		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetRange(0.01m, 0.2m)
			.SetDisplay("SAR Start", "Initial acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetRange(0.01m, 0.2m)
			.SetDisplay("SAR Increment", "Acceleration step for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetRange(0.05m, 1m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 11)
			.SetRange(1, 100)
			.SetDisplay("MA Period", "Period for moving average used in exit", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_previousSar = 0;
		_previousIsAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMax
		};

		var sma = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isPriceAboveSar = candle.ClosePrice > sarValue;
		var isCross = _previousSar > 0 && isPriceAboveSar != _previousIsAbove;

		if (isCross)
		{
			var volume = Volume + Math.Abs(Position);

			if (isPriceAboveSar && Position <= 0)
			{
				BuyMarket(volume);
				LogInfo($"Buy at {candle.ClosePrice}, SAR {sarValue}");
			}
			else if (!isPriceAboveSar && Position >= 0)
			{
				SellMarket(volume);
				LogInfo($"Sell at {candle.ClosePrice}, SAR {sarValue}");
			}
		}
		else if (Position > 0 && sarValue > candle.ClosePrice && candle.ClosePrice < maValue)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long at {candle.ClosePrice}, SAR {sarValue}, MA {maValue}");
		}

		_previousSar = sarValue;
		_previousIsAbove = isPriceAboveSar;
	}
}

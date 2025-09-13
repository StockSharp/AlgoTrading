namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Parabolic SAR Alert Strategy.
/// Opens long or short positions when Parabolic SAR flips relative to price.
/// </summary>
public class ParabolicSarAlertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialAcceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSar;
	private decimal? _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarAlertStrategy"/>.
	/// </summary>
	public ParabolicSarAlertStrategy()
	{
		_initialAcceleration = Param(nameof(InitialAcceleration), 0.02m)
			.SetDisplay("Initial Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
			.SetRange(0.01m, 0.1m)
			.SetCanOptimize(true);

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal InitialAcceleration
	{
		get => _initialAcceleration.Value;
		set => _initialAcceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_prevSar = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = InitialAcceleration,
			AccelerationMax = MaxAcceleration
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle with Parabolic SAR value.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="sarValue">Parabolic SAR value.</param>
	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevSar is not null && _prevClose is not null)
		{
			var crossUp = _prevSar > _prevClose && sarValue < candle.ClosePrice;
			var crossDown = _prevSar < _prevClose && sarValue > candle.ClosePrice;

			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Parabolic SAR switched below price: SAR {sarValue}, Close {candle.ClosePrice}");
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Parabolic SAR switched above price: SAR {sarValue}, Close {candle.ClosePrice}");
			}
		}

		_prevSar = sarValue;
		_prevClose = candle.ClosePrice;
	}
}

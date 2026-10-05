using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Reversal strategy.
/// When the SAR moves from above the close to below it the position turns long, and when it moves from below to above it turns short.
/// There is no other exit.
/// </summary>
public class ParabolicSarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialAcceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevSarAbove;

	/// <summary>
	/// Initial acceleration factor of the SAR.
	/// </summary>
	public decimal InitialAcceleration
	{
		get => _initialAcceleration.Value;
		set => _initialAcceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicSarReversalStrategy()
	{
		_initialAcceleration = Param(nameof(InitialAcceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Acceleration", "Initial acceleration factor of the SAR", "Indicators");

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Acceleration", "Maximum acceleration factor of the SAR", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevSarAbove = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSarAbove = null;

		var sar = new ParabolicSar
		{
			Acceleration = InitialAcceleration,
			AccelerationMax = MaxAcceleration,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished || !sarValue.IsFormed || sarValue.IsEmpty)
			return;

		var sar = sarValue.GetValue<decimal>();

		if (sar <= 0)
			return;

		var isSarAbove = sar > candle.ClosePrice;
		var wasSarAbove = _prevSarAbove;
		_prevSarAbove = isSarAbove;

		if (wasSarAbove is not bool previous || previous == isSarAbove || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (!isSarAbove && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (isSarAbove && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

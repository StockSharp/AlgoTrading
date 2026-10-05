using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR early buy with moving average exit.
/// A close crossing above the Parabolic SAR goes long and a close crossing below it goes short, reversing an opposite position.
/// A long is also closed early when the SAR is above the close and the close is below the MaPeriod simple moving average.
/// </summary>
public class ParabolicSarEarlyBuyMaBasedExitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _accelerationStep;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevSar;

	/// <summary>
	/// Initial SAR acceleration factor.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor increment.
	/// </summary>
	public decimal AccelerationStep
	{
		get => _accelerationStep.Value;
		set => _accelerationStep.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration factor.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Period of the exit moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public ParabolicSarEarlyBuyMaBasedExitStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration", "Initial SAR acceleration factor", "Indicators");

		_accelerationStep = Param(nameof(AccelerationStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration Step", "SAR acceleration factor increment", "Indicators");

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Acceleration", "Maximum SAR acceleration factor", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the exit moving average", "Indicators");

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
		_prevClose = null;
		_prevSar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevSar = null;

		var sar = new ParabolicSar
		{
			Acceleration = this.Acceleration,
			AccelerationStep = this.AccelerationStep,
			AccelerationMax = MaxAcceleration
		};
		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevSar = _prevSar;
		_prevClose = close;
		_prevSar = sar;

		if (prevClose is not decimal pc || prevSar is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = pc <= ps && close > sar;
		var crossDown = pc >= ps && close < sar;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && sar > close && close < ma)
			SellMarket(Position);
	}
}

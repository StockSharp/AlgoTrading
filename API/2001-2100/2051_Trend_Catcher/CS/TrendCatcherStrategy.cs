using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR trend catching strategy.
/// Uses Parabolic SAR flip with MA trend filter for entries.
/// </summary>
public class TrendCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _slowMa;
	private bool _isInitialized;
	private bool _isPriceAboveSarPrev;

	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendCatcherStrategy()
	{
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the slow moving average", "Moving Averages");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the fast moving average", "Moving Averages");

		_sarStep = Param(nameof(SarStep), 0.004m)
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Parabolic SAR");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Parabolic SAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_slowMa = default;
		_isInitialized = default;
		_isPriceAboveSarPrev = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isInitialized = false;

		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};
		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		Indicators.Add(_slowMa);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, fastMa, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(3, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent),
			isStopTrailing: true
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue fastMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!sarValue.IsFormed || !fastMaValue.IsFormed)
			return;

		var sar = sarValue.ToDecimal();
		var fastValue = fastMaValue.ToDecimal();

		var slowResult = _slowMa.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!slowResult.IsFormed)
			return;

		var slowValue = slowResult.ToDecimal();

		var isPriceAboveSar = candle.ClosePrice > sar;

		if (!_isInitialized)
		{
			_isPriceAboveSarPrev = isPriceAboveSar;
			_isInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Buy: SAR flips below price + fast MA above slow MA
		var buySignal = isPriceAboveSar && !_isPriceAboveSarPrev && fastValue > slowValue;
		// Sell: SAR flips above price + fast MA below slow MA
		var sellSignal = !isPriceAboveSar && _isPriceAboveSarPrev && fastValue < slowValue;

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_isPriceAboveSarPrev = isPriceAboveSar;
	}
}

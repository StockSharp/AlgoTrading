namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// IsConnected strategy: Parabolic SAR trend following.
/// Buys when close above SAR, sells when close below SAR.
/// </summary>
public class IsConnectedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _accelerationMax;

	private decimal _prevSar;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Acceleration { get => _acceleration.Value; set => _acceleration.Value = value; }
	public decimal AccelerationMax { get => _accelerationMax.Value; set => _accelerationMax.Value = value; }

	public IsConnectedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetDisplay("Acceleration", "SAR acceleration factor", "Indicators");
		_accelerationMax = Param(nameof(AccelerationMax), 0.2m)
			.SetDisplay("Acceleration Max", "SAR max acceleration", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var sar = new ParabolicSar { Acceleration = Acceleration, AccelerationMax = AccelerationMax };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (candle.ClosePrice > sarValue && _prevSar >= candle.ClosePrice && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < sarValue && _prevSar <= candle.ClosePrice && Position >= 0)
				SellMarket();
		}

		_prevSar = sarValue;
		_hasPrev = true;
	}
}

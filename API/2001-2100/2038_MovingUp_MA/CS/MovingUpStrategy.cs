using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with risk management via StartProtection.
/// </summary>
public class MovingUpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isInitialized;
	private bool _wasFastBelowSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingUpStrategy()
	{
		_fastLength = Param(nameof(FastLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "MA");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "MA");

		_stopLoss = Param(nameof(StopLoss), 250m)
			.SetGreaterThanZero()
			.SetDisplay("SL", "Stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("TP", "Take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
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
		_isInitialized = default;
		_wasFastBelowSlow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(StopLoss, UnitTypes.Absolute),
			new Unit(TakeProfit, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_wasFastBelowSlow = fast < slow;
			_isInitialized = true;
			return;
		}

		var isFastBelowSlow = fast < slow;

		if (_wasFastBelowSlow != isFastBelowSlow)
		{
			if (!isFastBelowSlow && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (isFastBelowSlow && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}

			_wasFastBelowSlow = isFastBelowSlow;
		}
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the OsHMA oscillator (difference of fast and slow Hull MA).
/// Trades on zero crossings or direction changes of the oscillator.
/// </summary>
public class OsHmaStrategy : Strategy
{
	public enum OsHmaModes
	{
		Breakdown,
		Twist
	}

	private readonly StrategyParam<int> _fastHma;
	private readonly StrategyParam<int> _slowHma;
	private readonly StrategyParam<OsHmaModes> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal _prevValue;
	private decimal _prevPrevValue;
	private int _count;

	public int FastHma { get => _fastHma.Value; set => _fastHma.Value = value; }
	public int SlowHma { get => _slowHma.Value; set => _slowHma.Value = value; }
	public OsHmaModes Mode { get => _mode.Value; set => _mode.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public OsHmaStrategy()
	{
		_fastHma = Param(nameof(FastHma), 13)
			.SetDisplay("Fast HMA", "Length of fast Hull Moving Average", "Indicators");

		_slowHma = Param(nameof(SlowHma), 26)
			.SetDisplay("Slow HMA", "Length of slow Hull Moving Average", "Indicators");

		_mode = Param(nameof(Mode), OsHmaModes.Twist)
			.SetDisplay("Mode", "Breakdown or Twist", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Target profit in points", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Loss limit in points", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevValue = 0;
		_prevPrevValue = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastHma = new HullMovingAverage { Length = FastHma };
		var slowHma = new HullMovingAverage { Length = SlowHma };

		SubscribeCandles(CandleType)
			.Bind(fastHma, slowHma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var current = fastValue - slowValue;
		_count++;

		if (_count < 3)
		{
			_prevPrevValue = _prevValue;
			_prevValue = current;
			return;
		}

		var buySignal = false;
		var sellSignal = false;

		switch (Mode)
		{
			case OsHmaModes.Breakdown:
				buySignal = _prevValue <= 0 && current > 0;
				sellSignal = _prevValue >= 0 && current < 0;
				break;
			case OsHmaModes.Twist:
				buySignal = _prevValue < _prevPrevValue && current > _prevValue;
				sellSignal = _prevValue > _prevPrevValue && current < _prevValue;
				break;
		}

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

		_prevPrevValue = _prevValue;
		_prevValue = current;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA slope reversal with absolute stop loss and take profit protection.
/// </summary>
public class ExpMovingAverageFnStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private int _count;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public ExpMovingAverageFnStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Absolute price distance from entry", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Absolute price distance from entry", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevPrevEma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaValue;
			return;
		}

		var turnUp = _prevEma < _prevPrevEma && emaValue > _prevEma;
		var turnDown = _prevEma > _prevPrevEma && emaValue < _prevEma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(-Position);
			BuyMarket(Volume);
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
	}
}

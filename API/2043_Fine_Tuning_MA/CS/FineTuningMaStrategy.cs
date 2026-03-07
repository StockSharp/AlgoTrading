using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fine Tuning MA strategy. Opens positions when the moving average changes direction.
/// </summary>
public class FineTuningMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private int _candleCount;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FineTuningMaStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of the moving average", "Parameters");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetDisplay("Take Profit, %", "Take profit level in percent", "Protection");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss, %", "Stop loss level in percent", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Parameters");
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
		_prev1 = default;
		_prev2 = default;
		_candleCount = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new ExponentialMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, ProcessCandle).Start();

		StartProtection(
			new Unit(StopLossPercent, UnitTypes.Percent),
			new Unit(TakeProfitPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_candleCount++;
		if (_candleCount <= 2)
		{
			_prev2 = _prev1;
			_prev1 = ma;
			return;
		}

		var wasRising = _prev1 > _prev2;
		var wasFalling = _prev1 < _prev2;

		if (wasRising && ma > _prev1 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (wasFalling && ma < _prev1 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = ma;
	}
}

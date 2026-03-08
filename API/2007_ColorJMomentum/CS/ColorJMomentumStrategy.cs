using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed momentum direction changes.
/// Opens long when momentum turns up, short when momentum turns down.
/// </summary>
public class ColorJMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private decimal _prevMom;
	private decimal _prevPrevMom;
	private int _count;

	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	public ColorJMomentumStrategy()
	{
		_momentumLength = Param(nameof(MomentumLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Period for momentum", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk management");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = 0;
		_prevPrevMom = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumLength };

		SubscribeCandles(CandleType)
			.Bind(momentum, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;
		if (_count < 3)
		{
			_prevPrevMom = _prevMom;
			_prevMom = momValue;
			return;
		}

		var wasDecreasing = _prevMom < _prevPrevMom;
		var nowIncreasing = momValue > _prevMom;
		var wasIncreasing = _prevMom > _prevPrevMom;
		var nowDecreasing = momValue < _prevMom;

		// Momentum turns up - go long
		if (wasDecreasing && nowIncreasing && EnableLong && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Momentum turns down - go short
		else if (wasIncreasing && nowDecreasing && EnableShort && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevMom = _prevMom;
		_prevMom = momValue;
	}
}

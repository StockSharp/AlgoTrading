using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Jurik moving average slope reversals, with fixed absolute protection.
/// Buys when JMA turns up, sells when JMA turns down.
/// </summary>
public class ColorJVariationStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaPeriod;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _prevJma;
	private decimal _prevPrevJma;
	private int _count;

	public int JmaPeriod { get => _jmaPeriod.Value; set => _jmaPeriod.Value = value; }
	public int JmaPhase { get => _jmaPhase.Value; set => _jmaPhase.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public ColorJVariationStrategy()
	{
		_jmaPeriod = Param(nameof(JmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("JMA Period", "JMA averaging period", "Indicator");

		_jmaPhase = Param(nameof(JmaPhase), 100)
			.SetDisplay("JMA Phase", "Phase for JMA", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Absolute stop distance from entry", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Absolute take-profit distance from entry", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevJma = 0;
		_prevPrevJma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var jma = new JurikMovingAverage
		{
			Length = JmaPeriod,
			Phase = JmaPhase
		};

		SubscribeCandles(CandleType)
			.Bind(jma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevJma = _prevJma;
			_prevJma = jmaValue;
			return;
		}

		var turnUp = _prevJma < _prevPrevJma && jmaValue > _prevJma;
		var turnDown = _prevJma > _prevPrevJma && jmaValue < _prevJma;

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

		_prevPrevJma = _prevJma;
		_prevJma = jmaValue;
	}
}

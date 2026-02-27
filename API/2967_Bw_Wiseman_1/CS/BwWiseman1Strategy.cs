using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Wiseman 1 strategy using Alligator-style triple EMA crossover.
/// </summary>
public class BwWiseman1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _lipsPeriod;

	private decimal? _prevLips;
	private decimal? _prevTeeth;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	public BwWiseman1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_jawPeriod = Param(nameof(JawPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Slow EMA (Jaw)", "Indicators");

		_teethPeriod = Param(nameof(TeethPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Medium EMA (Teeth)", "Indicators");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Fast EMA (Lips)", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLips = null;
		_prevTeeth = null;

		var jaw = new SmoothedMovingAverage { Length = JawPeriod };
		var teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		var lips = new SmoothedMovingAverage { Length = LipsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, teeth, lips, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawVal, decimal teethVal, decimal lipsVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevLips == null || _prevTeeth == null)
		{
			_prevLips = lipsVal;
			_prevTeeth = teethVal;
			return;
		}

		// Lips crosses above teeth → buy
		if (_prevLips.Value <= _prevTeeth.Value && lipsVal > teethVal)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Lips crosses below teeth → sell
		else if (_prevLips.Value >= _prevTeeth.Value && lipsVal < teethVal)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevLips = lipsVal;
		_prevTeeth = teethVal;
	}
}

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining SuperTrend with EMA crossover for trend confirmation.
/// </summary>
public class TrendConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendConfirmationStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Length", "ATR period for Supertrend", "Supertrend");

		_factor = Param(nameof(Factor), 3m)
			.SetDisplay("Factor", "Supertrend multiplier", "Supertrend");

		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast EMA length", "EMA");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow EMA length", "EMA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastEma = 0;
		_prevSlowEma = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stVal, IIndicatorValue fastVal, IIndicatorValue slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stVal is not SuperTrendIndicatorValue st)
			return;

		var fastEma = fastVal.GetValue<decimal>();
		var slowEma = slowVal.GetValue<decimal>();

		var upTrend = st.IsUpTrend;
		var downTrend = !upTrend;

		var macdBullish = fastEma > slowEma;
		var macdBearish = fastEma < slowEma;

		if (_prevFastEma == 0)
		{
			_prevFastEma = fastEma;
			_prevSlowEma = slowEma;
			return;
		}

		var prevCross = _prevFastEma - _prevSlowEma;
		var currCross = fastEma - slowEma;

		// Entry: SuperTrend + EMA confirmation
		if (upTrend && macdBullish && Position <= 0)
			BuyMarket();
		else if (downTrend && macdBearish && Position >= 0)
			SellMarket();
		// Exit on EMA crossover against position
		else if (prevCross >= 0 && currCross < 0 && Position > 0)
			SellMarket();
		else if (prevCross <= 0 && currCross > 0 && Position < 0)
			BuyMarket();

		_prevFastEma = fastEma;
		_prevSlowEma = slowEma;
	}
}

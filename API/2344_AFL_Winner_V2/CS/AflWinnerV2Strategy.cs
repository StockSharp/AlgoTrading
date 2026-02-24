using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the AFL Winner indicator approximation using a stochastic oscillator.
/// </summary>
public class AflWinnerV2Strategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevColor;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AflWinnerV2Strategy()
	{
		_kPeriod = Param<int>(nameof(KPeriod), 5).SetDisplay("%K Period", "%K Period", "General");
		_dPeriod = Param<int>(nameof(DPeriod), 3).SetDisplay("%D Period", "%D Period", "General");
		_highLevel = Param<decimal>(nameof(HighLevel), 40m).SetDisplay("High Level", "High Level", "General");
		_lowLevel = Param<decimal>(nameof(LowLevel), -40m).SetDisplay("Low Level", "Low Level", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = -1;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = KPeriod;
		stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stochValue is not IStochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		int color;

		if (k > d)
			color = (k > HighLevel || (k > LowLevel && d <= LowLevel)) ? 3 : 2;
		else
			color = (k < LowLevel || (d > HighLevel && k <= HighLevel)) ? 0 : 1;

		if (color == 3 && _prevColor != 3 && Position <= 0)
		{
			BuyMarket();
		}
		else if (color == 0 && _prevColor != 0 && Position >= 0)
		{
			SellMarket();
		}
		else if (color < 2 && Position > 0)
		{
			SellMarket();
		}
		else if (color > 1 && Position < 0)
		{
			BuyMarket();
		}

		_prevColor = color;
	}
}

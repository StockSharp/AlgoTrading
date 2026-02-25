using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VininI Trend LRMA strategy.
/// Computes a trend oscillator as deviation from linear regression.
/// Buys when oscillator crosses above upper level, sells when below lower level.
/// </summary>
public class VininITrendLrmaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _dnLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOsc;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DnLevel { get => _dnLevel.Value; set => _dnLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VininITrendLrmaStrategy()
	{
		_period = Param(nameof(Period), 13)
			.SetGreaterThanZero()
			.SetDisplay("LRMA period", "Linear regression period", "General");

		_upLevel = Param(nameof(UpLevel), 0.1m)
			.SetDisplay("Upper level", "Upper trigger level (percent)", "General");

		_dnLevel = Param(nameof(DnLevel), -0.1m)
			.SetDisplay("Lower level", "Lower trigger level (percent)", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevOsc = null;

		var lrma = new LinearReg { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lrma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lrma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (lrma == 0)
			return;

		// Compute trend oscillator as percentage deviation from LRMA
		var osc = (candle.ClosePrice - lrma) / lrma * 100m;

		if (_prevOsc is not null)
		{
			// Breakout mode
			if (osc > UpLevel && _prevOsc <= UpLevel && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (osc < DnLevel && _prevOsc >= DnLevel && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevOsc = osc;
	}
}

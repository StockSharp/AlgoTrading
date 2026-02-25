using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Schaff Trend Cycle indicator level crossovers.
/// </summary>
public class ColorSchaffJccxTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev;

	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorSchaffJccxTrendCycleStrategy()
	{
		_highLevel = Param(nameof(HighLevel), 75m)
			.SetDisplay("High Level", "Upper trigger level", "Signal");

		_lowLevel = Param(nameof(LowLevel), 25m)
			.SetDisplay("Low Level", "Lower trigger level", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stc = new SchaffTrendCycle();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stc);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stc)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev is null)
		{
			_prev = stc;
			return;
		}

		if (_prev > HighLevel && stc <= HighLevel && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prev < LowLevel && stc >= LowLevel && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prev = stc;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Schaff Trend Cycle level crossovers.
/// Buys when STC crosses above the high level, sells when below low level.
/// </summary>
public class ColorSchaffJjrsxTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevStc;

	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorSchaffJjrsxTrendCycleStrategy()
	{
		_highLevel = Param(nameof(HighLevel), 75m)
			.SetDisplay("High Level", "Upper threshold for signals", "Levels");

		_lowLevel = Param(nameof(LowLevel), 25m)
			.SetDisplay("Low Level", "Lower threshold for signals", "Levels");

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
		_prevStc = null;
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

		if (_prevStc is null)
		{
			_prevStc = stc;
			return;
		}

		if (_prevStc <= HighLevel && stc > HighLevel && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevStc >= LowLevel && stc < LowLevel && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevStc = stc;
	}
}

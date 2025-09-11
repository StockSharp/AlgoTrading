using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates accessing higher timeframe data with optional repaint control.
/// </summary>
public class SecurityRevisitedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe;
	private readonly StrategyParam<bool> _repaint;

	private decimal? _higherPrev;
	private decimal? _higherCurr;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}

	public bool Repaint
	{
		get => _repaint.Value;
		set => _repaint.Value = value;
	}

	public SecurityRevisitedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Timeframe", "Base timeframe", "General");

		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Higher Timeframe", "Higher timeframe", "General");

		_repaint = Param(nameof(Repaint), false)
			.SetDisplay("Repaint", "Use latest value or previous", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var higher = SubscribeCandles(HigherTimeframe);
		higher.WhenNew(ProcessHigher).Start();

		var main = SubscribeCandles(CandleType);
		main.Bind(ProcessMain).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, main);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessHigher(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherPrev = _higherCurr;
		_higherCurr = candle.ClosePrice;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = Repaint ? _higherCurr : _higherPrev;
		if (value == null)
			return;

		if (value > candle.ClosePrice && Position <= 0)
			BuyMarket();
		else if (value < candle.ClosePrice && Position >= 0)
			SellMarket();
	}
}


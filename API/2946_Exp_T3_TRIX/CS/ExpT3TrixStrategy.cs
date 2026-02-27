using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// T3 TRIX strategy using TRIX indicator for momentum signals.
/// Trades on TRIX zero-line crossovers.
/// </summary>
public class ExpT3TrixStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal? _prevTrix;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public ExpT3TrixStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "TRIX period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTrix = null;

		var trix = new Trix { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(trix, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var indArea = CreateChartArea();
		if (indArea != null)
		{
			DrawIndicator(indArea, trix);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal trixValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevTrix == null)
		{
			_prevTrix = trixValue;
			return;
		}

		// TRIX crosses above zero → buy
		if (_prevTrix.Value <= 0 && trixValue > 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// TRIX crosses below zero → sell
		else if (_prevTrix.Value >= 0 && trixValue < 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevTrix = trixValue;
	}
}

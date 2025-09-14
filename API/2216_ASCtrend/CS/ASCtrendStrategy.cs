using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ASCtrend strategy based on Williams %R reversals.
/// </summary>
public class ASCtrendStrategy : Strategy
{
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<DataType> _candleType;

	private bool _wasOversold;
	private bool _wasOverbought;

	/// <summary>
	/// Risk factor that adjusts threshold levels.
	/// </summary>
	public int Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public ASCtrendStrategy()
	{
		_risk = Param(nameof(Risk), 4)
			.SetRange(1, 50)
			.SetDisplay("Risk", "Risk parameter", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var wpr = new WilliamsPercentRange
		{
			Length = 3 + Risk * 2
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value2 = 100m - Math.Abs(wpr);
		var x1 = 67m + Risk;
		var x2 = 33m - Risk;

		if (value2 < x2)
			_wasOversold = true;
		else if (value2 > x1)
			_wasOverbought = true;

		if (_wasOversold && value2 > x1 && Position >= 0)
		{
			SellMarket();
			_wasOversold = false;
			_wasOverbought = false;
			return;
		}

		if (_wasOverbought && value2 < x2 && Position <= 0)
		{
			BuyMarket();
			_wasOversold = false;
			_wasOverbought = false;
		}
	}
}

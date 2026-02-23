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
/// Williams %R based extreme reversals.
/// Enters long when all indicators show deep oversold.
/// Enters short when all indicators show strong overbought.
/// </summary>
public class MasterMind3Strategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod1;
	private readonly StrategyParam<int> _wprPeriod2;
	private readonly StrategyParam<int> _wprPeriod3;
	private readonly StrategyParam<int> _wprPeriod4;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for the first Williams %R indicator.
	/// </summary>
	public int WprPeriod1
	{
		get => _wprPeriod1.Value;
		set => _wprPeriod1.Value = value;
	}

	/// <summary>
	/// Period for the second Williams %R indicator.
	/// </summary>
	public int WprPeriod2
	{
		get => _wprPeriod2.Value;
		set => _wprPeriod2.Value = value;
	}

	/// <summary>
	/// Period for the third Williams %R indicator.
	/// </summary>
	public int WprPeriod3
	{
		get => _wprPeriod3.Value;
		set => _wprPeriod3.Value = value;
	}

	/// <summary>
	/// Period for the fourth Williams %R indicator.
	/// </summary>
	public int WprPeriod4
	{
		get => _wprPeriod4.Value;
		set => _wprPeriod4.Value = value;
	}

	/// <summary>
	/// The type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MasterMind3Strategy()
	{
		_wprPeriod1 = Param(nameof(WprPeriod1), 26)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period 1", "Length of the first Williams %R indicator", "WilliamsR")
			
			.SetOptimize(10, 50, 5);

		_wprPeriod2 = Param(nameof(WprPeriod2), 27)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period 2", "Length of the second Williams %R indicator", "WilliamsR")
			
			.SetOptimize(10, 50, 5);

		_wprPeriod3 = Param(nameof(WprPeriod3), 29)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period 3", "Length of the third Williams %R indicator", "WilliamsR")
			
			.SetOptimize(10, 50, 5);

		_wprPeriod4 = Param(nameof(WprPeriod4), 30)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period 4", "Length of the fourth Williams %R indicator", "WilliamsR")
			
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
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

		var wpr1 = new WilliamsR { Length = WprPeriod1 };
		var wpr2 = new WilliamsR { Length = WprPeriod2 };
		var wpr3 = new WilliamsR { Length = WprPeriod3 };
		var wpr4 = new WilliamsR { Length = WprPeriod4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wpr1, wpr2, wpr3, wpr4, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr1);
			DrawIndicator(area, wpr2);
			DrawIndicator(area, wpr3);
			DrawIndicator(area, wpr4);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr1, decimal wpr2, decimal wpr3, decimal wpr4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isBuySignal = wpr1 <= -80m && wpr2 <= -80m && wpr3 <= -80m && wpr4 <= -80m;
		var isSellSignal = wpr1 >= -20m && wpr2 >= -20m && wpr3 >= -20m && wpr4 >= -20m;

		if (isBuySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isSellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}

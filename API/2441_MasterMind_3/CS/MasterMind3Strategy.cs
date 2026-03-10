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
	private readonly StrategyParam<int> _rsiPeriod1;
	private readonly StrategyParam<int> _rsiPeriod2;
	private readonly StrategyParam<int> _rsiPeriod3;
	private readonly StrategyParam<int> _rsiPeriod4;
	private readonly StrategyParam<DataType> _candleType;
	private bool _wasOversold;
	private bool _wasOverbought;

	/// <summary>
	/// Period for the first Williams %R indicator.
	/// </summary>
	public int RsiPeriod1
	{
		get => _rsiPeriod1.Value;
		set => _rsiPeriod1.Value = value;
	}

	/// <summary>
	/// Period for the second Williams %R indicator.
	/// </summary>
	public int RsiPeriod2
	{
		get => _rsiPeriod2.Value;
		set => _rsiPeriod2.Value = value;
	}

	/// <summary>
	/// Period for the third Williams %R indicator.
	/// </summary>
	public int RsiPeriod3
	{
		get => _rsiPeriod3.Value;
		set => _rsiPeriod3.Value = value;
	}

	/// <summary>
	/// Period for the fourth Williams %R indicator.
	/// </summary>
	public int RsiPeriod4
	{
		get => _rsiPeriod4.Value;
		set => _rsiPeriod4.Value = value;
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
		_rsiPeriod1 = Param(nameof(RsiPeriod1), 26)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period 1", "Length of the first RSI indicator", "RSI")
			
			.SetOptimize(10, 50, 5);

		_rsiPeriod2 = Param(nameof(RsiPeriod2), 27)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period 2", "Length of the second RSI indicator", "RSI")
			
			.SetOptimize(10, 50, 5);

		_rsiPeriod3 = Param(nameof(RsiPeriod3), 29)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period 3", "Length of the third RSI indicator", "RSI")
			
			.SetOptimize(10, 50, 5);

		_rsiPeriod4 = Param(nameof(RsiPeriod4), 30)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period 4", "Length of the fourth RSI indicator", "RSI")
			
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
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
		_wasOversold = false;
		_wasOverbought = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi1 = new RelativeStrengthIndex { Length = RsiPeriod1 };
		var rsi2 = new RelativeStrengthIndex { Length = RsiPeriod2 };
		var rsi3 = new RelativeStrengthIndex { Length = RsiPeriod3 };
		var rsi4 = new RelativeStrengthIndex { Length = RsiPeriod4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi1, rsi2, rsi3, rsi4, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi1);
			DrawIndicator(area, rsi2);
			DrawIndicator(area, rsi3);
			DrawIndicator(area, rsi4);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi1, decimal rsi2, decimal rsi3, decimal rsi4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isOversold = rsi1 <= 35m && rsi2 <= 35m && rsi3 <= 35m && rsi4 <= 35m;
		var isOverbought = rsi1 >= 65m && rsi2 >= 65m && rsi3 >= 65m && rsi4 >= 65m;
		var isBuySignal = isOversold && !_wasOversold;
		var isSellSignal = isOverbought && !_wasOverbought;

		if (isBuySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isSellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_wasOversold = isOversold;
		_wasOverbought = isOverbought;
	}
}

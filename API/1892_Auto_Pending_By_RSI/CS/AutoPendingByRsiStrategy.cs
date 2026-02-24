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
/// Enters after RSI stays in extreme zones for several consecutive candles.
/// Buys after sustained oversold, sells after sustained overbought.
/// </summary>
public class AutoPendingByRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _matchCount;
	private readonly StrategyParam<DataType> _candleType;

	private int _overboughtCount;
	private int _oversoldCount;

	/// <summary>RSI calculation period.</summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	/// <summary>RSI overbought level.</summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	/// <summary>RSI oversold level.</summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	/// <summary>Number of consecutive candles before placing orders.</summary>
	public int MatchCount { get => _matchCount.Value; set => _matchCount.Value = value; }
	/// <summary>Candle type for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public AutoPendingByRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetOptimize(7, 21, 7);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
			.SetOptimize(60m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
			.SetOptimize(20m, 40m, 5m);

		_matchCount = Param(nameof(MatchCount), 3)
			.SetDisplay("Match Count", "Consecutive candles before entry", "General")
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
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
		_overboughtCount = 0;
		_oversoldCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, Process)
			.Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (rsi < RsiOversold)
		{
			_oversoldCount++;
			_overboughtCount = 0;
		}
		else if (rsi > RsiOverbought)
		{
			_overboughtCount++;
			_oversoldCount = 0;
		}
		else
		{
			_overboughtCount = 0;
			_oversoldCount = 0;
		}

		if (_oversoldCount >= MatchCount && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_oversoldCount = 0;
		}

		if (_overboughtCount >= MatchCount && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_overboughtCount = 0;
		}
	}
}

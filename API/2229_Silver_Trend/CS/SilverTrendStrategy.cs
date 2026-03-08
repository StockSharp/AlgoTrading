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
/// Strategy based on SilverTrend indicator -- trades reversals based on
/// price channel breakouts with a risk-based filter.
/// </summary>
public class SilverTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private bool? _uptrend;
	private bool? _prevUptrend;

	public int Ssp { get => _ssp.Value; set => _ssp.Value = value; }
	public int Risk { get => _risk.Value; set => _risk.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SilverTrendStrategy()
	{
		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "Lookback length for price channel", "Indicator");

		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Risk factor used to tighten the channel", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_highest = null;
		_lowest = null;
		_uptrend = null;
		_prevUptrend = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Ssp };
		_lowest = new Lowest { Length = Ssp };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maxHigh, decimal minLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var k = 33 - Risk;
		var smin = minLow + (maxHigh - minLow) * k / 100m;
		var smax = maxHigh - (maxHigh - minLow) * k / 100m;

		var uptrend = _uptrend ?? false;

		if (candle.ClosePrice < smin)
			uptrend = false;
		else if (candle.ClosePrice > smax)
			uptrend = true;

		var reversed = _uptrend is not null && uptrend != _uptrend;

		if (IsFormedAndOnlineAndAllowTrading() && reversed)
		{
			if (uptrend && Position <= 0)
				BuyMarket();
			else if (!uptrend && Position >= 0)
				SellMarket();
		}

		_prevUptrend = _uptrend;
		_uptrend = uptrend;
	}
}

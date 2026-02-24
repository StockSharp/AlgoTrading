using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R cloud breakout strategy converted from the MetaTrader expert advisor.
/// Looks for %R crossings above -80 to go long and below -20 to go short.
/// Positions are reversed when an opposite signal appears.
/// </summary>
public class WprCustomCloudSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _williamsR;
	private decimal? _previousWpr;
	private decimal? _olderWpr;

	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public WprCustomCloudSimpleStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R");

		_overboughtLevel = Param(nameof(OverboughtLevel), -20m)
			.SetDisplay("Overbought Level", "%R level that marks overbought conditions", "Williams %R");

		_oversoldLevel = Param(nameof(OversoldLevel), -80m)
			.SetDisplay("Oversold Level", "%R level that marks oversold conditions", "Williams %R");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Williams %R", "Data");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_williamsR = new WilliamsR { Length = WprPeriod };
		_previousWpr = null;
		_olderWpr = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_williamsR.IsFormed)
		{
			_olderWpr = _previousWpr;
			_previousWpr = wprValue;
			return;
		}

		if (_previousWpr is not null && _olderWpr is not null)
		{
			var prev = _previousWpr.Value;
			var prevPrev = _olderWpr.Value;

			var crossedAboveOversold = prevPrev < OversoldLevel && prev > OversoldLevel;
			var crossedBelowOverbought = prevPrev > OverboughtLevel && prev < OverboughtLevel;

			var volume = Volume;
			if (volume <= 0)
				volume = 1;

			if (crossedAboveOversold)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				if (Position <= 0)
					BuyMarket(volume);
			}
			else if (crossedBelowOverbought)
			{
				if (Position > 0)
					SellMarket(Position);

				if (Position >= 0)
					SellMarket(volume);
			}
		}

		_olderWpr = _previousWpr;
		_previousWpr = wprValue;
	}
}

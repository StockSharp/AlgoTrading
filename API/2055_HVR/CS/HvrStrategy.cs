using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Historical Volatility Ratio (HVR).
/// Compares short-term volatility against long-term volatility.
/// Buys when short-term vol exceeds long-term, sells when below.
/// </summary>
public class HvrStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _ratioThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _shortSd;
	private StandardDeviation _longSd;
	private decimal? _prevClose;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public decimal RatioThreshold { get => _ratioThreshold.Value; set => _ratioThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HvrStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Short HV Period", "Bars for short-term volatility", "Parameters");

		_longPeriod = Param(nameof(LongPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long HV Period", "Bars for long-term volatility", "Parameters");

		_ratioThreshold = Param(nameof(RatioThreshold), 1m)
			.SetDisplay("Ratio Threshold", "HVR level for trade direction", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculation", "General");
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
		_shortSd = default;
		_longSd = default;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_shortSd = new StandardDeviation { Length = ShortPeriod };
		_longSd = new StandardDeviation { Length = LongPeriod };

		Indicators.Add(_shortSd);
		Indicators.Add(_longSd);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is not decimal prevClose || prevClose <= 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var logReturn = (decimal)Math.Log((double)(candle.ClosePrice / prevClose));
		_prevClose = candle.ClosePrice;

		var shortResult = _shortSd.Process(logReturn, candle.OpenTime, true);
		var longResult = _longSd.Process(logReturn, candle.OpenTime, true);

		if (!shortResult.IsFormed || !longResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shortVal = shortResult.ToDecimal();
		var longVal = longResult.ToDecimal();

		if (longVal == 0)
			return;

		var ratio = shortVal / longVal;

		if (ratio > RatioThreshold && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (ratio < RatioThreshold && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}

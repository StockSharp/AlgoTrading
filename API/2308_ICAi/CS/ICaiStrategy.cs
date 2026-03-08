using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the ICAi adaptive moving average.
/// Computes an adaptive MA using SMA and StdDev, trades on slope reversal.
/// </summary>
public class ICaiStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private StandardDeviation _std;
	private decimal? _prevIcai;
	private decimal? _prevSlope;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ICaiStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Indicator smoothing length", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		_ma = null;
		_std = null;
		_prevIcai = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevIcai = null;
		_prevSlope = null;

		_ma = new SimpleMovingAverage { Length = Length };
		_std = new StandardDeviation { Length = Length };

		Indicators.Add(_ma);
		Indicators.Add(_std);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var t = candle.OpenTime;

		var maResult = _ma.Process(price, t, true);
		var stdResult = _std.Process(price, t, true);

		if (!_ma.IsFormed || !_std.IsFormed)
			return;

		var maVal = maResult.GetValue<decimal>();
		var stdVal = stdResult.GetValue<decimal>();

		var prev = _prevIcai ?? maVal;
		var diff = prev - maVal;
		var powDxma = diff * diff;
		var powStd = stdVal * stdVal;

		decimal koeff = 0m;
		if (powDxma >= powStd && powDxma != 0m)
			koeff = 1m - powStd / powDxma;

		var icai = prev + koeff * (maVal - prev);
		_prevIcai = icai;

		if (_prevSlope is null)
		{
			_prevSlope = 0m;
			return;
		}

		var slope = icai - prev;

		// Slope reversal: was negative, now positive -> buy
		if (_prevSlope <= 0 && slope > 0 && Position <= 0)
			BuyMarket();
		// Slope reversal: was positive, now negative -> sell
		else if (_prevSlope >= 0 && slope < 0 && Position >= 0)
			SellMarket();

		_prevSlope = slope;
	}
}

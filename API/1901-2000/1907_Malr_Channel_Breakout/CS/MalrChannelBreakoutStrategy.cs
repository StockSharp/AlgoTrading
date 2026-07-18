using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MALR channel breakout strategy.
/// Enters long when price breaks above the upper MALR band and short when breaking below the lower band.
/// </summary>
public class MalrChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _channelReversal;
	private readonly StrategyParam<decimal> _channelBreakout;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private WeightedMovingAverage _lwma;
	private StandardDeviation _stdDev;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _prevClose;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal ChannelReversal { get => _channelReversal.Value; set => _channelReversal.Value = value; }
	public decimal ChannelBreakout { get => _channelBreakout.Value; set => _channelBreakout.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MalrChannelBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("MA", "Moving average period", "General")
			.SetOptimize(50, 200, 10);

		_channelReversal = Param(nameof(ChannelReversal), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Reversal", "Channel reversal width", "General")
			.SetOptimize(0.5m, 2m, 0.1m);

		_channelBreakout = Param(nameof(ChannelBreakout), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout", "Channel breakout width", "General")
			.SetOptimize(0.5m, 2m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
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
		_sma = default;
		_lwma = default;
		_stdDev = default;
		_prevUpper = null;
		_prevLower = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };
		_lwma = new WeightedMovingAverage { Length = MaPeriod };
		_stdDev = new StandardDeviation { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _lwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaResult = _sma.Process(candle.ClosePrice, candle.OpenTime, true);
		var lwmaResult = _lwma.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!smaResult.IsFormed || !lwmaResult.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var smaVal = smaResult.ToDecimal();
		var lwmaVal = lwmaResult.ToDecimal();
		var ff = 3m * lwmaVal - 2m * smaVal;

		var deviation = candle.ClosePrice - ff;
		var stdResult = _stdDev.Process(deviation, candle.OpenTime, true);

		if (!stdResult.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = ff;
			_prevLower = ff;
			return;
		}

		var std = stdResult.ToDecimal();
		var upper = ff + std * (ChannelReversal + ChannelBreakout);
		var lower = ff - std * (ChannelReversal + ChannelBreakout);

		if (_prevUpper.HasValue && _prevLower.HasValue && _prevClose.HasValue)
		{
			// Price breaks above upper channel
			if (_prevClose.Value <= _prevUpper.Value && candle.ClosePrice > upper && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Price breaks below lower channel
			else if (_prevClose.Value >= _prevLower.Value && candle.ClosePrice < lower && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range Filter strategy based on ATR.
/// </summary>
public class RangeFilterDwStrategy : Strategy
{
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _filter;
	private bool _isInitialized;

	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RangeFilterDwStrategy()
	{
		_rangePeriod = Param(nameof(RangePeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Period", "ATR period for range calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_rangeMultiplier = Param(nameof(RangeMultiplier), 2.618m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier applied to ATR", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_filter = 0m;
		_isInitialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new ATR { Length = RangePeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, (candle, atrValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var range = atrValue * RangeMultiplier;

				if (!_isInitialized)
				{
					_filter = (candle.HighPrice + candle.LowPrice) / 2m;
					_isInitialized = true;
					return;
				}

				if (candle.HighPrice - range > _filter)
					_filter = candle.HighPrice - range;
				else if (candle.LowPrice + range < _filter)
					_filter = candle.LowPrice + range;

				var highBand = _filter + range;
				var lowBand = _filter - range;

				if (candle.ClosePrice > highBand && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (candle.ClosePrice < lowBand && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
}
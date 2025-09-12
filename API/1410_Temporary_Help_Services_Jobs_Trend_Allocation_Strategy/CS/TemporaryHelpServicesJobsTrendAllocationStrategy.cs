
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend allocation strategy based on monthly Temporary Help Services Jobs data.
/// Enters long when value rises and exits when it falls.
/// </summary>
public class TemporaryHelpServicesJobsTrendAllocationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TemporaryHelpServicesJobsTrendAllocationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(31).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0m)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var trendUp = candle.ClosePrice > _prevClose;
		var trendDown = candle.ClosePrice < _prevClose;

		if (trendUp && Position == 0)
		{
			BuyMarket();
		}
		else if (trendDown && Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
	}
}

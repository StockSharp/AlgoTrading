using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Support/resistance level breakout strategy.
/// Tracks highest high and lowest low over a lookback period.
/// Buys on breakout above resistance, sells on breakdown below support.
/// </summary>
public class ExternalLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevResistance;
	private decimal _prevSupport;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExternalLevelStrategy()
	{
		_lookback = Param(nameof(Lookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Support/resistance lookback period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevResistance = 0;
		_prevSupport = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		_prevResistance = 0;
		_prevSupport = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal resistance, decimal support)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevResistance == 0)
		{
			_prevResistance = resistance;
			_prevSupport = support;
			return;
		}

		if (candle.ClosePrice > _prevResistance && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < _prevSupport && Position >= 0)
			SellMarket();

		_prevResistance = resistance;
		_prevSupport = support;
	}
}

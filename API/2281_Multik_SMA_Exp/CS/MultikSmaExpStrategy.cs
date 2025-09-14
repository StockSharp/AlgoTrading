using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian strategy based on moving average slope.
/// Buys when SMA decreases for two consecutive segments and sells when it increases.
/// Positions are closed when the SMA slope reverses.
/// </summary>
public class MultikSmaExpStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _ma0;
	private decimal? _ma1;
	private decimal? _ma2;
	private decimal? _ma3;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultikSmaExpStrategy()
	{
		_period = Param(nameof(Period), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_ma0 = _ma1 = _ma2 = _ma3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = Period };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, (candle, smaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				_ma3 = _ma2;
				_ma2 = _ma1;
				_ma1 = _ma0;
				_ma0 = smaValue;

				if (_ma3 is null || _ma2 is null || _ma1 is null)
					return;

				var dsma1 = _ma1.Value - _ma2.Value;
				var dsma2 = _ma2.Value - _ma3.Value;

				// Exit conditions
				if (dsma1 > 0 && Position > 0)
					SellMarket(Position);
				if (dsma1 < 0 && Position < 0)
					BuyMarket(-Position);

				// Entry conditions
				if (dsma2 < 0 && dsma1 < 0 && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (dsma2 > 0 && dsma1 > 0 && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}

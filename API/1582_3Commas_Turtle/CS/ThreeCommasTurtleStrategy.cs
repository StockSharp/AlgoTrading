using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified turtle breakout strategy.
/// </summary>
public class ThreeCommasTurtleStrategy : Strategy
{
	private readonly StrategyParam<int> _periodFast;
	private readonly StrategyParam<int> _periodSlow;
	private readonly StrategyParam<int> _periodExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpperFast;
	private decimal _prevLowerFast;
	private decimal _prevClose;

	public int PeriodFast
	{
		get => _periodFast.Value;
		set => _periodFast.Value = value;
	}

	public int PeriodSlow
	{
		get => _periodSlow.Value;
		set => _periodSlow.Value = value;
	}

	public int PeriodExit
	{
		get => _periodExit.Value;
		set => _periodExit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ThreeCommasTurtleStrategy()
	{
		_periodFast = Param(nameof(PeriodFast), 20)
			.SetDisplay("Period Fast", "Fast channel period", "Channels");
		_periodSlow = Param(nameof(PeriodSlow), 20)
			.SetDisplay("Period Slow", "Slow channel period", "Channels");
		_periodExit = Param(nameof(PeriodExit), 10)
			.SetDisplay("Period Exit", "Exit channel period", "Channels");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpperFast = 0m;
		_prevLowerFast = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var upperFast = new Highest { Length = PeriodFast };
		var lowerFast = new Lowest { Length = PeriodFast };
		var upperSlow = new Highest { Length = PeriodSlow };
		var lowerSlow = new Lowest { Length = PeriodSlow };
		var upperExit = new Highest { Length = PeriodExit };
		var lowerExit = new Lowest { Length = PeriodExit };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(upperFast, lowerFast, upperSlow, lowerSlow, upperExit, lowerExit, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal upFast, decimal lowFast, decimal upSlow, decimal lowSlow, decimal upExit, decimal lowExit)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose <= _prevUpperFast && candle.ClosePrice > _prevUpperFast && candle.ClosePrice > upSlow && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevClose >= _prevLowerFast && candle.ClosePrice < _prevLowerFast && candle.ClosePrice < lowSlow && Position >= 0)
		{
			SellMarket();
		}

		if (Position > 0 && candle.ClosePrice < lowExit)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > upExit)
			BuyMarket();

		_prevUpperFast = upFast;
		_prevLowerFast = lowFast;
		_prevClose = candle.ClosePrice;
	}
}

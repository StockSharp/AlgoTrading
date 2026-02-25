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
/// Trend detection using Fibonacci period moving averages.
/// </summary>
public class FibonacciAutoTrendScouterStrategy : Strategy
{
	private readonly StrategyParam<int> _smallPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSmall;
	private decimal _prevMedium;
	private bool _isReady;

	public int SmallPeriod { get => _smallPeriod.Value; set => _smallPeriod.Value = value; }
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FibonacciAutoTrendScouterStrategy()
	{
		_smallPeriod = Param(nameof(SmallPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Small Period", "Small EMA period", "General");
		_mediumPeriod = Param(nameof(MediumPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Medium Period", "Medium EMA period", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSmall = 0;
		_prevMedium = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaSmall = new ExponentialMovingAverage { Length = SmallPeriod };
		var emaMedium = new ExponentialMovingAverage { Length = MediumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaSmall, emaMedium, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaSmall);
			DrawIndicator(area, emaMedium);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal small, decimal medium)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevSmall = small;
			_prevMedium = medium;
			_isReady = true;
			return;
		}

		// Crossover detection
		var crossUp = _prevSmall <= _prevMedium && small > medium;
		var crossDown = _prevSmall >= _prevMedium && small < medium;

		if (crossUp && Position <= 0)
			BuyMarket();
		else if (crossDown && Position >= 0)
			SellMarket();

		_prevSmall = small;
		_prevMedium = medium;
	}
}

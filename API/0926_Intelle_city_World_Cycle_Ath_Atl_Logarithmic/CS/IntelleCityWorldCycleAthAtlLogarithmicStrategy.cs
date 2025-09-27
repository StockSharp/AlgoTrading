using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Pi Cycle concept using scaled moving averages.
/// </summary>
public class IntelleCityWorldCycleAthAtlLogarithmicStrategy : Strategy
{
	private readonly StrategyParam<int> _athLongLength;
	private readonly StrategyParam<int> _athShortLength;
	private readonly StrategyParam<int> _atlLongLength;
	private readonly StrategyParam<int> _atlShortLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAthLong;
	private decimal _prevAthShort;
	private decimal _prevAtlLong;
	private decimal _prevAtlShort;

	public int AthLongLength { get => _athLongLength.Value; set => _athLongLength.Value = value; }
	public int AthShortLength { get => _athShortLength.Value; set => _athShortLength.Value = value; }
	public int AtlLongLength { get => _atlLongLength.Value; set => _atlLongLength.Value = value; }
	public int AtlShortLength { get => _atlShortLength.Value; set => _atlShortLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IntelleCityWorldCycleAthAtlLogarithmicStrategy()
	{
		_athLongLength = Param(nameof(AthLongLength), 350)
			.SetGreaterThanZero()
			.SetDisplay("ATH Long MA", "Length for ATH long moving average", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(200, 500, 50);

		_athShortLength = Param(nameof(AthShortLength), 111)
			.SetGreaterThanZero()
			.SetDisplay("ATH Short MA", "Length for ATH short moving average", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_atlLongLength = Param(nameof(AtlLongLength), 471)
			.SetGreaterThanZero()
			.SetDisplay("ATL Long MA", "Length for ATL long moving average", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(300, 600, 50);

		_atlShortLength = Param(nameof(AtlShortLength), 150)
			.SetGreaterThanZero()
			.SetDisplay("ATL Short MA", "Length for ATL short moving average", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevAthLong = _prevAthShort = _prevAtlLong = _prevAtlShort = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var maAthLong = new SimpleMovingAverage { Length = AthLongLength };
		var maAthShort = new SimpleMovingAverage { Length = AthShortLength };
		var maAtlLong = new SimpleMovingAverage { Length = AtlLongLength };
		var maAtlShort = new ExponentialMovingAverage { Length = AtlShortLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(maAthLong, maAthShort, maAtlLong, maAtlShort, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, maAthLong);
			DrawIndicator(area, maAthShort);
			DrawIndicator(area, maAtlLong);
			DrawIndicator(area, maAtlShort);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal athLong, decimal athShort, decimal atlLong, decimal atlShort)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var scaledAthLong = athLong * 2m;
		var scaledAtlLong = atlLong * 0.745m;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (_prevAthLong >= _prevAthShort && scaledAthLong < athShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (_prevAtlLong <= _prevAtlShort && scaledAtlLong > atlShort && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		_prevAthLong = scaledAthLong;
		_prevAthShort = athShort;
		_prevAtlLong = scaledAtlLong;
		_prevAtlShort = atlShort;
	}
}

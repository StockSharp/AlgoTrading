namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Heikin Ashi ROC Percentile Strategy.
/// </summary>
public class HeikinAshiRocPercentileStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;

	private SimpleMovingAverage _sma;
	private RateOfChange _roc;
	private Highest _highest;
	private Lowest _lowest;

	private readonly Queue<decimal> _rocHighValues = new();
	private readonly Queue<decimal> _rocLowValues = new();

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevRoc;
	private decimal _prevUpperKill;
	private decimal _prevLowerKill;
	private bool _isFirst = true;

	private const int RocHighLength = 50;
	private const int PercentileLength = 10;

	public HeikinAshiRocPercentileStrategy()
	{
		_rocLength = Param(nameof(RocLength), 100)
		.SetDisplay("ROC Length", "Lookback period for SMA and ROC", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Date", "Start date filter", "General");
	}

	public int RocLength { get => _rocLength.Value; set => _rocLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = 0m;
		_prevHaClose = 0m;
		_prevRoc = 0m;
		_prevUpperKill = 0m;
		_prevLowerKill = 0m;
		_isFirst = true;

		_rocHighValues.Clear();
		_rocLowValues.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = RocLength };
		_roc = new RateOfChange { Length = RocLength };
		_highest = new Highest { Length = RocHighLength };
		_lowest = new Lowest { Length = RocHighLength };

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

		StartProtection(
		takeProfit: null,
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (candle.OpenTime < StartDate)
		return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _prevHaOpen == 0m ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_prevHaOpen + _prevHaClose) / 2m;

		var smaValue = _sma.Process(haClose, candle.OpenTime, true).ToDecimal();
		var rocValue = _roc.Process(smaValue, candle.OpenTime, true).ToDecimal();

		var highestValue = _highest.Process(rocValue, candle.OpenTime, true).ToDecimal();
		var lowestValue = _lowest.Process(rocValue, candle.OpenTime, true).ToDecimal();

		_rocHighValues.Enqueue(highestValue);
		if (_rocHighValues.Count > PercentileLength)
		_rocHighValues.Dequeue();

		_rocLowValues.Enqueue(lowestValue);
		if (_rocLowValues.Count > PercentileLength)
		_rocLowValues.Dequeue();

		if (_rocHighValues.Count < PercentileLength || _rocLowValues.Count < PercentileLength)
		{
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevRoc = rocValue;
			return;
		}

		var upperKill = GetPercentile(_rocHighValues, 0.75m);
		var lowerKill = GetPercentile(_rocLowValues, 0.25m);

		if (_isFirst)
		{
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevRoc = rocValue;
			_prevUpperKill = upperKill;
			_prevLowerKill = lowerKill;
			_isFirst = false;
			return;
		}

		var crossUp = rocValue > lowerKill && _prevRoc <= _prevLowerKill;
		var crossDown = rocValue < upperKill && _prevRoc >= _prevUpperKill;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevRoc = rocValue;
		_prevUpperKill = upperKill;
		_prevLowerKill = lowerKill;
	}

	private static decimal GetPercentile(Queue<decimal> values, decimal percentile)
	{
		var arr = values.ToArray();
		Array.Sort(arr);
		var pos = (arr.Length - 1) * percentile;
		var idx = (int)pos;
		var frac = pos - idx;
		var lower = arr[idx];
		var upper = arr[Math.Min(idx + 1, arr.Length - 1)];
		return lower + (upper - lower) * frac;
	}
}

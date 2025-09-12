using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple higher timeframe moving averages with dynamic smoothing.
/// </summary>
public class TripleMaHtfDynamicSmoothingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _htf1;
	private readonly StrategyParam<DataType> _htf2;
	private readonly StrategyParam<DataType> _htf3;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _length3;

	private SimpleMovingAverage _ma1;
	private SimpleMovingAverage _ma2;
	private SimpleMovingAverage _ma3;

	private SimpleMovingAverage _smooth1;
	private SimpleMovingAverage _smooth2;
	private SimpleMovingAverage _smooth3;

	private decimal _ma1Value;
	private decimal _ma2Value;
	private decimal _ma3Value;
	private decimal _ma1Prev;
	private decimal _ma2Prev;
	private decimal _ma3Prev;

	public TripleMaHtfDynamicSmoothingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Base timeframe", "General");

		_htf1 = Param(nameof(HigherTimeFrame1), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("HTF1", "First higher timeframe", "Trend");

		_htf2 = Param(nameof(HigherTimeFrame2), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("HTF2", "Second higher timeframe", "Trend");

		_htf3 = Param(nameof(HigherTimeFrame3), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("HTF3", "Third higher timeframe", "Trend");

		_length1 = Param(nameof(Length1), 21)
			.SetDisplay("MA1 Length", "Length for first MA", "Trend");

		_length2 = Param(nameof(Length2), 21)
			.SetDisplay("MA2 Length", "Length for second MA", "Trend");

		_length3 = Param(nameof(Length3), 50)
			.SetDisplay("MA3 Length", "Length for third MA", "Trend");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType HigherTimeFrame1
	{
		get => _htf1.Value;
		set => _htf1.Value = value;
	}

	public DataType HigherTimeFrame2
	{
		get => _htf2.Value;
		set => _htf2.Value = value;
	}

	public DataType HigherTimeFrame3
	{
		get => _htf3.Value;
		set => _htf3.Value = value;
	}

	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	public int Length3
	{
		get => _length3.Value;
		set => _length3.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeFrame1), (Security, HigherTimeFrame2), (Security, HigherTimeFrame3)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = new SimpleMovingAverage { Length = Length1 };
		_ma2 = new SimpleMovingAverage { Length = Length2 };
		_ma3 = new SimpleMovingAverage { Length = Length3 };

		var baseSpan = (TimeSpan)CandleType.Arg;
		var span1 = (TimeSpan)HigherTimeFrame1.Arg;
		var span2 = (TimeSpan)HigherTimeFrame2.Arg;
		var span3 = (TimeSpan)HigherTimeFrame3.Arg;

		_smooth1 = new SimpleMovingAverage { Length = Math.Max(1, (int)(span1.TotalMinutes / baseSpan.TotalMinutes)) };
		_smooth2 = new SimpleMovingAverage { Length = Math.Max(1, (int)(span2.TotalMinutes / baseSpan.TotalMinutes)) };
		_smooth3 = new SimpleMovingAverage { Length = Math.Max(1, (int)(span3.TotalMinutes / baseSpan.TotalMinutes)) };

		SubscribeCandles(HigherTimeFrame1)
			.Bind(_ma1, ProcessHtf1)
			.Start();

		SubscribeCandles(HigherTimeFrame2)
			.Bind(_ma2, ProcessHtf2)
			.Start();

		SubscribeCandles(HigherTimeFrame3)
			.Bind(_ma3, ProcessHtf3)
			.Start();

		var baseSub = SubscribeCandles(CandleType);
		baseSub
			.Bind(ProcessBase)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHtf1(ICandleMessage candle, decimal ma)
	{
		var value = _smooth1.Process(ma, candle.OpenTime, candle.State == CandleStates.Finished).ToDecimal();
		if (_smooth1.IsFormed)
		{
			_ma1Prev = _ma1Value;
			_ma1Value = value;
		}
	}

	private void ProcessHtf2(ICandleMessage candle, decimal ma)
	{
		var value = _smooth2.Process(ma, candle.OpenTime, candle.State == CandleStates.Finished).ToDecimal();
		if (_smooth2.IsFormed)
		{
			_ma2Prev = _ma2Value;
			_ma2Value = value;
		}
	}

	private void ProcessHtf3(ICandleMessage candle, decimal ma)
	{
		var value = _smooth3.Process(ma, candle.OpenTime, candle.State == CandleStates.Finished).ToDecimal();
		if (_smooth3.IsFormed)
		{
			_ma3Prev = _ma3Value;
			_ma3Value = value;
		}
	}

	private void ProcessBase(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_smooth1.IsFormed || !_smooth2.IsFormed || !_smooth3.IsFormed)
			return;

		var buySignal = _ma1Prev <= _ma2Prev && _ma1Value > _ma2Value && _ma3Value > _ma3Prev;
		var sellSignal = _ma1Prev >= _ma2Prev && _ma1Value < _ma2Value && _ma3Value < _ma3Prev;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && _ma1Value < _ma2Value)
			SellMarket(Position);
		else if (Position < 0 && _ma1Value > _ma2Value)
			BuyMarket(-Position);
	}
}

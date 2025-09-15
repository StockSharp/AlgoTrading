using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades using Relative Vigor Index on three timeframes.
/// </summary>
public class TripleRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;
	private readonly StrategyParam<decimal> _volume;

	private RelativeVigorIndex _rvi1;
	private RelativeVigorIndex _rvi2;
	private RelativeVigorIndex _rvi3;
	private SimpleMovingAverage _signal1;
	private SimpleMovingAverage _signal2;
	private SimpleMovingAverage _signal3;

	private int _trend1;
	private int _trend2;
	private int _trend3;

	private decimal? _prevRvi3;
	private decimal? _prevSignal3;

	/// <summary>
	/// RVI period.
	/// </summary>
	public int RviPeriod
	{
		get => _rviPeriod.Value;
		set => _rviPeriod.Value = value;
	}

	/// <summary>
	/// Highest timeframe.
	/// </summary>
	public DataType CandleType1
	{
		get => _candleType1.Value;
		set => _candleType1.Value = value;
	}

	/// <summary>
	/// Middle timeframe.
	/// </summary>
	public DataType CandleType2
	{
		get => _candleType2.Value;
		set => _candleType2.Value = value;
	}

	/// <summary>
	/// Trading timeframe.
	/// </summary>
	public DataType CandleType3
	{
		get => _candleType3.Value;
		set => _candleType3.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="TripleRviStrategy"/>.
	/// </summary>
	public TripleRviStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Period of RVI", "General")
			.SetCanOptimize(true);

		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Timeframe 1", "Higher timeframe", "General");

		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Timeframe 2", "Middle timeframe", "General");

		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Timeframe 3", "Trading timeframe", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rvi1 = default;
		_rvi2 = default;
		_rvi3 = default;
		_signal1 = default;
		_signal2 = default;
		_signal3 = default;

		_trend1 = default;
		_trend2 = default;
		_trend3 = default;

		_prevRvi3 = default;
		_prevSignal3 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi1 = new RelativeVigorIndex { Length = RviPeriod };
		_rvi2 = new RelativeVigorIndex { Length = RviPeriod };
		_rvi3 = new RelativeVigorIndex { Length = RviPeriod };
		_signal1 = new SimpleMovingAverage { Length = 4 };
		_signal2 = new SimpleMovingAverage { Length = 4 };
		_signal3 = new SimpleMovingAverage { Length = 4 };

		var sub1 = SubscribeCandles(CandleType1);
		sub1.WhenNew(ProcessCandle1).Start();

		var sub2 = SubscribeCandles(CandleType2);
		sub2.WhenNew(ProcessCandle2).Start();

		var sub3 = SubscribeCandles(CandleType3);
		sub3.WhenNew(ProcessCandle3).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub3);
			DrawIndicator(area, _rvi3);
			DrawIndicator(area, _signal3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviVal = _rvi1.Process(candle);
		var signalVal = _signal1.Process(rviVal);

		if (!rviVal.IsFinal || !signalVal.IsFinal)
			return;

		var rvi = rviVal.ToDecimal();
		var signal = signalVal.ToDecimal();

		_trend1 = rvi > signal ? 1 : rvi < signal ? -1 : 0;
	}

	private void ProcessCandle2(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviVal = _rvi2.Process(candle);
		var signalVal = _signal2.Process(rviVal);

		if (!rviVal.IsFinal || !signalVal.IsFinal)
			return;

		var rvi = rviVal.ToDecimal();
		var signal = signalVal.ToDecimal();

		_trend2 = rvi > signal ? 1 : rvi < signal ? -1 : 0;
	}

	private void ProcessCandle3(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviVal = _rvi3.Process(candle);
		var signalVal = _signal3.Process(rviVal);

		if (!rviVal.IsFinal || !signalVal.IsFinal || !IsFormedAndOnlineAndAllowTrading())
			return;

		var rvi = rviVal.ToDecimal();
		var signal = signalVal.ToDecimal();

		_trend3 = rvi > signal ? 1 : rvi < signal ? -1 : 0;

		if (_prevRvi3 is decimal prevRvi && _prevSignal3 is decimal prevSignal)
		{
			var crossedDown = prevRvi > prevSignal && rvi <= signal;
			var crossedUp = prevRvi < prevSignal && rvi >= signal;

			if (crossedDown && _trend1 > 0 && _trend2 > 0 && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (crossedUp && _trend1 < 0 && _trend2 < 0 && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		if (Position > 0 && (_trend1 < 0 || _trend2 < 0 || _trend3 < 0))
			SellMarket(Position);
		else if (Position < 0 && (_trend1 > 0 || _trend2 > 0 || _trend3 > 0))
			BuyMarket(-Position);

		_prevRvi3 = rvi;
		_prevSignal3 = signal;
	}
}

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StepMA NRTR trend-following strategy.
/// </summary>
public class StepMaNrtrStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _kv;
	private readonly StrategyParam<int> _stepSize;
	private readonly StrategyParam<bool> _useHighLow;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _ranges = new();
	private decimal _smax1;
	private decimal _smin1;
	private int _trend1;
	private bool _first = true;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Kv { get => _kv.Value; set => _kv.Value = value; }
	public int StepSize { get => _stepSize.Value; set => _stepSize.Value = value; }
	public bool UseHighLow { get => _useHighLow.Value; set => _useHighLow.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StepMaNrtrStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Volatility length", "Indicator");

		_kv = Param(nameof(Kv), 1m)
			.SetDisplay("Sensitivity", "Sensitivity factor", "Indicator");

		_stepSize = Param(nameof(StepSize), 0)
			.SetDisplay("Step Size", "Constant step size, 0 - auto", "Indicator");

		_useHighLow = Param(nameof(UseHighLow), true)
			.SetDisplay("Use High/Low", "Use high/low range", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for processing", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ranges.Clear();
		_smax1 = 0;
		_smin1 = 0;
		_trend1 = 0;
		_first = true;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var warmup = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(warmup, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _warmupVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		_ranges.Enqueue(range);

		if (_ranges.Count > Length)
			_ranges.Dequeue();

		if (_ranges.Count < Length)
			return;

		decimal step;

		if (StepSize == 0)
		{
			var atrMax = _ranges.Max();
			var atrMin = _ranges.Min();
			step = 0.5m * Kv * (atrMax + atrMin);
		}
		else
			step = Kv * StepSize;

		if (step == 0)
			return;

		var sizeP = step;
		var size2P = 2m * step;

		if (_first)
		{
			_trend1 = 0;
			_smax1 = candle.LowPrice + size2P;
			_smin1 = candle.HighPrice - size2P;
			_first = false;
		}

		decimal smax0, smin0;

		if (UseHighLow)
		{
			smax0 = candle.LowPrice + size2P;
			smin0 = candle.HighPrice - size2P;
		}
		else
		{
			smax0 = candle.ClosePrice + size2P;
			smin0 = candle.ClosePrice - size2P;
		}

		var trend0 = _trend1;

		if (candle.ClosePrice > _smax1)
			trend0 = 1;
		else if (candle.ClosePrice < _smin1)
			trend0 = -1;

		if (trend0 > 0)
		{
			if (smin0 < _smin1)
				smin0 = _smin1;
		}
		else
		{
			if (smax0 > _smax1)
				smax0 = _smax1;
		}

		var buySignal = trend0 > 0 && _trend1 < 0;
		var sellSignal = trend0 < 0 && _trend1 > 0;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_smax1 = smax0;
		_smin1 = smin0;
		_trend1 = trend0;
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining SuperTrend, MACD and VWAP for trend confirmation.
/// </summary>
public class TrendConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private MovingAverageConvergenceDivergence _macd;
	private VolumeWeightedMovingAverage _vwap;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _isFirst = true;

	public TrendConfirmationStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Length", "ATR period for Supertrend.", "Supertrend");

		_factor = Param(nameof(Factor), 3m)
			.SetDisplay("Factor", "Supertrend multiplier.", "Supertrend");

		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "MACD fast MA length.", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "MACD slow MA length.", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "MACD signal length.", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_supertrend = null;
		_macd = null;
		_vwap = null;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_isFirst = true;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_supertrend = new() { Length = AtrPeriod, Multiplier = Factor };
		_macd = new() { ShortPeriod = FastLength, LongPeriod = SlowLength, SignalPeriod = SignalLength };
		_vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_supertrend, _macd, _vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue, decimal macd, decimal signal, decimal histogram, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st = (SuperTrendIndicatorValue)supertrendValue;
		bool upTrend = st.IsUpTrend;
		bool downTrend = !upTrend;

		bool confirmUpTrend = upTrend && macd > signal;
		bool confirmDownTrend = downTrend && macd < signal;

		bool priceAboveVwap = candle.ClosePrice > vwap;
		bool priceBelowVwap = candle.ClosePrice < vwap;

		if (_isFirst)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_isFirst = false;
		}

		if (confirmUpTrend && priceAboveVwap && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (confirmDownTrend && priceBelowVwap && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (_prevMacd >= _prevSignal && macd < signal && Position > 0)
			SellMarket(Position);
		else if (_prevMacd <= _prevSignal && macd > signal && Position < 0)
			BuyMarket(Math.Abs(Position));

		_prevMacd = macd;
		_prevSignal = signal;
	}
}

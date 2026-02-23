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
/// Dynamic averaging strategy using Stochastic oscillator with volatility filter.
/// </summary>
public class DynamicAveragingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	private StandardDeviation _stdDev;
	private SimpleMovingAverage _stdDevSma;

	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private decimal? _previousK;
	private decimal? _previousK2;
	private decimal _currentVolume;
	private decimal _positionPrice;

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DynamicAveragingStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume for new positions", "Trading")
			.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetDisplay("Stochastic Length", "Lookback for %K", "Indicators")
			.SetGreaterThanZero();

		_stdDevPeriod = Param(nameof(StdDevPeriod), 20)
			.SetDisplay("StdDev Length", "Lookback for the standard deviation filter", "Indicators")
			.SetGreaterThanZero();

		_oversoldLevel = Param(nameof(OversoldLevel), 25m)
			.SetDisplay("Oversold Level", "%K threshold for long entries", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 75m)
			.SetDisplay("Overbought Level", "%K threshold for short entries", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "Market Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highHistory.Clear();
		_lowHistory.Clear();
		_closeHistory.Clear();
		_previousK = null;
		_previousK2 = null;
		_currentVolume = TradeVolume;
		_positionPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentVolume = TradeVolume;

		_stdDev = new StandardDeviation { Length = StdDevPeriod };
		_stdDevSma = new SimpleMovingAverage { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);
		_closeHistory.Add(candle.ClosePrice);

		var period = StochasticKPeriod;
		while (_highHistory.Count > period) _highHistory.RemoveAt(0);
		while (_lowHistory.Count > period) _lowHistory.RemoveAt(0);
		while (_closeHistory.Count > period + 10) _closeHistory.RemoveAt(0);

		// Calculate Stochastic %K manually
		decimal? currentK = null;
		if (_highHistory.Count >= period && _lowHistory.Count >= period)
		{
			var highest = _highHistory.Max();
			var lowest = _lowHistory.Min();
			var range = highest - lowest;
			currentK = range > 0 ? ((candle.ClosePrice - lowest) / range) * 100m : 50m;
		}

		// Process StdDev
		var stdInput = new DecimalIndicatorValue(_stdDev, candle.ClosePrice, candle.ServerTime) { IsFinal = true };
		var stdOutput = _stdDev.Process(stdInput);

		decimal stdDevVal = 0;
		decimal stdDevAvg = 0;
		if (!stdOutput.IsEmpty)
		{
			stdDevVal = stdOutput.IsEmpty ? 0 : stdOutput.ToDecimal();
			var smaInput = new DecimalIndicatorValue(_stdDevSma, stdDevVal, candle.ServerTime) { IsFinal = true };
			var smaOutput = _stdDevSma.Process(smaInput);
			if (!smaOutput.IsEmpty)
				stdDevAvg = smaOutput.ToDecimal();
		}

		if (currentK == null || !_stdDev.IsFormed || !_stdDevSma.IsFormed)
		{
			if (currentK.HasValue)
			{
				_previousK2 = _previousK;
				_previousK = currentK;
			}
			return;
		}

		var k = currentK.Value;

		// Volatility filter: only trade when current stddev <= average stddev
		if (stdDevVal <= stdDevAvg && _previousK.HasValue && _previousK2.HasValue)
		{
			var slope = _previousK.Value - _previousK2.Value;

			if (k < OversoldLevel && slope > 0)
			{
				// Long signal
				if (Position <= 0)
				{
					var vol = _currentVolume + Math.Abs(Position);
					if (vol > 0)
					{
						BuyMarket(vol);
						_positionPrice = candle.ClosePrice;
					}
				}
			}
			else if (k > OverboughtLevel && slope < 0)
			{
				// Short signal
				if (Position >= 0)
				{
					var vol = _currentVolume + Math.Abs(Position);
					if (vol > 0)
					{
						SellMarket(vol);
						_positionPrice = candle.ClosePrice;
					}
				}
			}
		}

		_previousK2 = _previousK;
		_previousK = k;
	}
}

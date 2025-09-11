using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range filter strategy with fixed risk and reward.
/// Calculates a smooth range and opens positions when price breaks target bands.
/// </summary>
public class RangeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _samplingPeriod;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<decimal> _riskPoints;
	private readonly StrategyParam<decimal> _rewardPoints;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _avrng;
	private ExponentialMovingAverage _smrngEma;

	private decimal _prevSrc;
	private bool _hasPrevSrc;
	private decimal _filt;
	private bool _hasFilt;
	private decimal _entryPrice;
	private int _dailyTrades;
	private int _lastTradeDay;

	/// <summary>
	/// Sampling period for range calculation.
	/// </summary>
	public int SamplingPeriod
	{
		get => _samplingPeriod.Value;
		set => _samplingPeriod.Value = value;
	}

	/// <summary>
	/// Range multiplier.
	/// </summary>
	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal RiskPoints
	{
		get => _riskPoints.Value;
		set => _riskPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal RewardPoints
	{
		get => _rewardPoints.Value;
		set => _rewardPoints.Value = value;
	}

	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RangeFilterStrategy()
	{
		_samplingPeriod = Param(nameof(SamplingPeriod), 100)
			.SetDisplay("Sampling Period", "Period for range calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_rangeMultiplier = Param(nameof(RangeMultiplier), 3m)
			.SetDisplay("Range Multiplier", "Multiplier for range", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_riskPoints = Param(nameof(RiskPoints), 50m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_rewardPoints = Param(nameof(RewardPoints), 100m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 10m);

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5)
			.SetDisplay("Max Trades Per Day", "Daily trade limit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_avrng = new ExponentialMovingAverage { Length = SamplingPeriod };
		_smrngEma = new ExponentialMovingAverage { Length = SamplingPeriod * 2 - 1 };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Day;
		if (_lastTradeDay != day)
		{
			_dailyTrades = 0;
			_lastTradeDay = day;
		}

		var src = candle.ClosePrice;

		if (!_hasPrevSrc)
		{
			_prevSrc = src;
			_hasPrevSrc = true;
			return;
		}

		var diff = Math.Abs(src - _prevSrc);
		_prevSrc = src;

		var avrng = _avrng.Process(diff);
		if (!avrng.IsFinal)
			return;

		var smooth = _smrngEma.Process(avrng.GetValue<decimal>());
		if (!smooth.IsFinal)
			return;

		var smrng = smooth.GetValue<decimal>() * RangeMultiplier;

		var newFilt = src;
		if (_hasFilt)
		{
			newFilt = src > _filt ? Math.Max(src - smrng, _filt) : Math.Min(src + smrng, _filt);
		}

		_filt = newFilt;
		_hasFilt = true;

		var hband = _filt + smrng;
		var lband = _filt - smrng;

		if (Position > 0)
		{
			if (candle.LowPrice <= _entryPrice - RiskPoints || candle.HighPrice >= _entryPrice + RewardPoints)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);
			if (candle.HighPrice >= _entryPrice + RiskPoints || candle.LowPrice <= _entryPrice - RewardPoints)
			{
				BuyMarket(absPos);
			}
		}

		if (_dailyTrades >= MaxTradesPerDay)
			return;

		var volume = Volume + Math.Abs(Position);

		if (Position <= 0 && candle.ClosePrice > hband)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_dailyTrades++;
		}
		else if (Position >= 0 && candle.ClosePrice < lband)
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_dailyTrades++;
		}
	}
}

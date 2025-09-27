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
/// Awesome Oscillator + RAVI crossover strategy ported from MT5 expert advisor.
/// </summary>
public class RaviAoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<SmoothMethods> _fastMethod;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<SmoothMethods> _slowMethod;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;

	private IIndicator _fastAverage;
	private IIndicator _slowAverage;
	private AwesomeOscillator _ao;

	private decimal? _aoPrev;
	private decimal? _aoPrevPrev;
	private decimal? _raviPrev;
	private decimal? _raviPrevPrev;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _pipSize;
	private bool _isLongPosition;

	public RaviAoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe to analyze", "General");

		_stopLossPips = Param(nameof(StopLossPips), 15m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance", "Risk")
			.SetRange(0m, 1000m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 45m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance", "Risk")
			.SetRange(0m, 2000m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
			.SetRange(0m, 1000m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing update", "Risk")
			.SetRange(0m, 1000m);

		_fastMethod = Param(nameof(FastMethod), SmoothMethods.Exponential)
			.SetDisplay("Fast Method", "Smoothing method for fast RAVI average", "RAVI");

		_fastLength = Param(nameof(FastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Length of the fast RAVI average", "RAVI");

		_slowMethod = Param(nameof(SlowMethod), SmoothMethods.Exponential)
			.SetDisplay("Slow Method", "Smoothing method for slow RAVI average", "RAVI");

		_slowLength = Param(nameof(SlowLength), 65)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Length of the slow RAVI average", "RAVI");

		_appliedPrice = Param(nameof(AppliedPrices), AppliedPrices.Close)
			.SetDisplay("Applied Price", "Price source for RAVI", "RAVI");

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Short Period", "Fast period for Awesome Oscillator", "Awesome Oscillator");

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Long Period", "Slow period for Awesome Oscillator", "Awesome Oscillator");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public SmoothMethods FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public SmoothMethods SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public AppliedPrices AppliedPrices
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public int AoShortPeriod
	{
		get => _aoShortPeriod.Value;
		set => _aoShortPeriod.Value = value;
	}

	public int AoLongPeriod
	{
		get => _aoLongPeriod.Value;
		set => _aoLongPeriod.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_fastAverage = CreateMovingAverage(FastMethod, FastLength);
		_slowAverage = CreateMovingAverage(SlowMethod, SlowLength);
		_ao = new AwesomeOscillator
		{
			ShortPeriod = AoShortPeriod,
			LongPeriod = AoLongPeriod
		};

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ao);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ApplyTrailing(candle);
		if (CheckExit(candle))
			return;

		var price = GetPrice(candle, AppliedPrices);

		var fastValue = _fastAverage.Process(price, candle.OpenTime, true);
		var slowValue = _slowAverage.Process(price, candle.OpenTime, true);
		var aoValue = _ao.Process(candle.HighPrice, candle.LowPrice);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !aoValue.IsFinal)
		{
			UpdateHistory(null, null);
			return;
		}

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		if (slow == 0m)
		{
			UpdateHistory(null, null);
			return;
		}

		var ravi = 100m * (fast - slow) / slow;
		var ao = aoValue.GetValue<decimal>();

		var hasHistory = _aoPrevPrev.HasValue && _aoPrev.HasValue && _raviPrevPrev.HasValue && _raviPrev.HasValue;
		var canTrade = Position == 0 && hasHistory && IsFormedAndOnlineAndAllowTrading();

		if (canTrade)
		{
			var bullish = _aoPrevPrev.Value < 0m && _aoPrev.Value > 0m && _raviPrevPrev.Value < 0m && _raviPrev.Value > 0m;
			var bearish = _aoPrevPrev.Value > 0m && _aoPrev.Value < 0m && _raviPrevPrev.Value > 0m && _raviPrev.Value < 0m;

			if (bullish)
			{
				BuyMarket();
				InitializePositionState(candle.ClosePrice, true);
			}
			else if (bearish)
			{
				SellMarket();
				InitializePositionState(candle.ClosePrice, false);
			}
		}

		UpdateHistory(ao, ravi);
	}

	private void InitializePositionState(decimal price, bool isLong)
	{
		_entryPrice = price;
		_isLongPosition = isLong;

		_stopPrice = StopLossPips > 0m ? price + (isLong ? -1m : 1m) * StopLossPips * _pipSize : null;
		_takePrice = TakeProfitPips > 0m ? price + (isLong ? 1m : -1m) * TakeProfitPips * _pipSize : null;
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _entryPrice is null || Position == 0)
			return;

		var trailing = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (_isLongPosition)
		{
			var gain = candle.ClosePrice - _entryPrice.Value;
			if (gain > trailing + step)
			{
				var trigger = candle.ClosePrice - (trailing + step);
				if (_stopPrice is not decimal stop || stop < trigger)
				{
					_stopPrice = candle.ClosePrice - trailing;
				}
			}
		}
		else
		{
			var gain = _entryPrice.Value - candle.ClosePrice;
			if (gain > trailing + step)
			{
				var trigger = candle.ClosePrice + (trailing + step);
				if (_stopPrice is not decimal stop || stop > trigger)
				{
					_stopPrice = candle.ClosePrice + trailing;
				}
			}
		}
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice is null)
			return false;

		if (_isLongPosition)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}
		else
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_isLongPosition = false;
	}

	private void ResetState()
	{
		ResetTradeState();
		_aoPrev = null;
		_aoPrevPrev = null;
		_raviPrev = null;
		_raviPrevPrev = null;
		_pipSize = 0m;
	}

	private void UpdateHistory(decimal? ao, decimal? ravi)
	{
		if (ao.HasValue)
		{
			_aoPrevPrev = _aoPrev;
			_aoPrev = ao;
		}

		if (ravi.HasValue)
		{
			_raviPrevPrev = _raviPrev;
			_raviPrev = ravi;
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;
		var adjust = (decimals == 3 || decimals == 5) ? 10m : 1m;
		var pip = step * adjust;
		return pip == 0m ? 0.0001m : pip;
	}

	private static IIndicator CreateMovingAverage(SmoothMethods method, int length)
	{
		return method switch
		{
			SmoothMethods.Simple => new SimpleMovingAverage { Length = length },
			SmoothMethods.Exponential => new EMA { Length = length },
			SmoothMethods.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothMethods.Weighted => new WeightedMovingAverage { Length = length },
			_ => new EMA { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPrices appliedPrice)
	{
		return appliedPrice switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			AppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrices.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			res = (res + candle.HighPrice) / 2m;
		else
			res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	public enum SmoothMethods
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	public enum AppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}
}
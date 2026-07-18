using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout strategy using ATR-based thresholds and trailing stop.
/// </summary>
public class IuRangeTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrTargetFactor;
	private readonly StrategyParam<decimal> _atrRangeFactor;
	private readonly StrategyParam<int> _cooldownDays;
	private readonly StrategyParam<DataType> _candleType;

	private bool _previousRangeCond;
	private decimal _rangeHigh;
	private decimal _rangeLow;
	private decimal? _sl0;
	private decimal? _trailingSl;
	private decimal _entryPrice;
	private DateTime _nextEntryDate;

	/// <summary>
	/// Lookback period for range detection.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Multiplier for trailing stop step.
	/// </summary>
	public decimal AtrTargetFactor
	{
		get => _atrTargetFactor.Value;
		set => _atrTargetFactor.Value = value;
	}

	/// <summary>
	/// ATR multiplier to validate range.
	/// </summary>
	public decimal AtrRangeFactor
	{
		get => _atrRangeFactor.Value;
		set => _atrRangeFactor.Value = value;
	}

	/// <summary>
	/// Minimum days between entries.
	/// </summary>
	public int CooldownDays
	{
		get => _cooldownDays.Value;
		set => _cooldownDays.Value = value;
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
	/// Initializes a new instance of <see cref="IuRangeTradingStrategy"/>.
	/// </summary>
	public IuRangeTradingStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 10)
			.SetDisplay("Range Length", "Lookback period for range detection.", "Parameters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Parameters");

		_atrTargetFactor = Param(nameof(AtrTargetFactor), 2.0m)
			.SetDisplay("ATR Target Factor", "Multiplier for trailing stop step.", "Parameters");

		_atrRangeFactor = Param(nameof(AtrRangeFactor), 1.75m)
			.SetDisplay("ATR Range Factor", "ATR multiplier to validate range.", "Parameters");

		_cooldownDays = Param(nameof(CooldownDays), 3)
			.SetDisplay("Cooldown Days", "Minimum days between entries.", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousRangeCond = false;
		_rangeHigh = 0m;
		_rangeLow = 0m;
		_sl0 = null;
		_trailingSl = null;
		_entryPrice = 0m;
		_nextEntryDate = DateTime.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousRangeCond = false;
		_rangeHigh = 0m;
		_rangeLow = 0m;
		_sl0 = null;
		_trailingSl = null;
		_entryPrice = 0m;
		_nextEntryDate = DateTime.MinValue;


		var highest = new Highest { Length = RangeLength };
		var lowest = new Lowest { Length = RangeLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rangeCond = (highestValue - lowestValue) <= atrValue * AtrRangeFactor;

		// Track range boundaries
		if (rangeCond && !_previousRangeCond && Position == 0)
		{
			_rangeHigh = highestValue;
			_rangeLow = lowestValue;
		}
		else if (rangeCond && _previousRangeCond && Position == 0)
		{
			_rangeHigh = Math.Max(_rangeHigh, highestValue);
			_rangeLow = Math.Min(_rangeLow, lowestValue);
		}

		// Entry logic: breakout from range
		if (Position == 0 && _rangeHigh != 0 && _rangeLow != 0 && candle.OpenTime.Date >= _nextEntryDate)
		{
			if (candle.ClosePrice > _rangeHigh)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_sl0 = _entryPrice - atrValue * AtrTargetFactor;
				_trailingSl = _entryPrice + atrValue * AtrTargetFactor;
				_nextEntryDate = candle.OpenTime.Date.AddDays(CooldownDays);
			}
			else if (candle.ClosePrice < _rangeLow)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_sl0 = _entryPrice + atrValue * AtrTargetFactor;
				_trailingSl = _entryPrice - atrValue * AtrTargetFactor;
				_nextEntryDate = candle.OpenTime.Date.AddDays(CooldownDays);
			}
		}

		// Exit logic with trailing stop
		if (Position > 0 && _sl0.HasValue && _trailingSl.HasValue)
		{
			if (candle.HighPrice > _trailingSl.Value)
			{
				var step = atrValue * AtrTargetFactor;
				_sl0 = _trailingSl - step;
				_trailingSl += step;
			}

			if (candle.LowPrice <= _sl0.Value)
			{
				SellMarket();
				_sl0 = _trailingSl = null;
				_rangeHigh = 0m;
				_rangeLow = 0m;
			}
		}
		else if (Position < 0 && _sl0.HasValue && _trailingSl.HasValue)
		{
			if (candle.LowPrice < _trailingSl.Value)
			{
				var step = atrValue * AtrTargetFactor;
				_sl0 = _trailingSl + step;
				_trailingSl -= step;
			}

			if (candle.HighPrice >= _sl0.Value)
			{
				BuyMarket();
				_sl0 = _trailingSl = null;
				_rangeHigh = 0m;
				_rangeLow = 0m;
			}
		}

		_previousRangeCond = rangeCond;
	}
}

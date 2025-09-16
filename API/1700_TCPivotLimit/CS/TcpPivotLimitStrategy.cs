using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot point trading strategy based on previous day's support and resistance levels.
/// Places limit orders at pivot levels and manages positions with stop loss and take profit.
/// </summary>
public class TcpPivotLimitStrategy : Strategy
{
	private readonly StrategyParam<int> _targetVariant;
	private readonly StrategyParam<bool> _intradayClose;
	private readonly StrategyParam<bool> _modifyStopLoss;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	private decimal _pivot;
	private decimal _r1, _r2, _r3;
	private decimal _s1, _s2, _s3;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _firstTarget;
	private bool _isLong;

	/// <summary>
	/// Variant of pivot levels combination (1-5).
	/// </summary>
	public int TargetVariant
	{
		get => _targetVariant.Value;
		set => _targetVariant.Value = value;
	}

	/// <summary>
	/// Close position at 23:00.
	/// </summary>
	public bool IntradayClose
	{
		get => _intradayClose.Value;
		set => _intradayClose.Value = value;
	}

	/// <summary>
	/// Move stop loss to the first target after it is reached.
	/// </summary>
	public bool ModifyStopLoss
	{
		get => _modifyStopLoss.Value;
		set => _modifyStopLoss.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal LotVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TcpPivotLimitStrategy()
	{
		_targetVariant = Param(nameof(TargetVariant), 1)
			.SetDisplay("Target Variant", "Choose pivot level combination", "General")
			.SetOptimize(1, 5, 1);

		_intradayClose = Param(nameof(IntradayClose), false)
			.SetDisplay("Intraday Close", "Close position at 23:00", "Risk");

		_modifyStopLoss = Param(nameof(ModifyStopLoss), false)
			.SetDisplay("Modify Stop Loss", "Move stop to first target after it is reached", "Risk");

		_volume = Param(nameof(LotVolume), 1m)
			.SetDisplay("Volume", "Order volume", "General")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentDay = default;
		_pivot = _r1 = _r2 = _r3 = _s1 = _s2 = _s3 = 0;
		_stopPrice = _takePrice = _firstTarget = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.UtcDateTime.Date;

		if (_currentDay != day)
		{
			if (_currentDay != default)
				CalculatePivot();

			_currentDay = day;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			return;
		}

		_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
		_dayLow = Math.Min(_dayLow, candle.LowPrice);
		_dayClose = candle.ClosePrice;

		if (_pivot == 0)
			return;

		if (Position == 0)
		{
			TryEnter(candle);
		}
		else
		{
			CheckExit(candle);
		}

		if (IntradayClose && candle.CloseTime.TimeOfDay >= new TimeSpan(23, 0, 0) && Position != 0)
		{
			ClosePosition();
		}
	}

	private void CalculatePivot()
	{
		_pivot = (_dayHigh + _dayLow + _dayClose) / 3m;
		var range = _dayHigh - _dayLow;
		_r1 = 2m * _pivot - _dayLow;
		_s1 = 2m * _pivot - _dayHigh;
		_r2 = _pivot + range;
		_s2 = _pivot - range;
		_r3 = _dayHigh + 2m * (_pivot - _dayLow);
		_s3 = _dayLow - 2m * (_dayHigh - _pivot);
	}

	private void TryEnter(ICandleMessage candle)
	{
		var buyLevel = GetBuyLevels(out var buyStop, out var buyTake, out var buyFirstTarget);
		var sellLevel = GetSellLevels(out var sellStop, out var sellTake, out var sellFirstTarget);

		if (buyLevel != 0 && candle.LowPrice <= buyLevel && Position <= 0)
		{
			_isLong = true;
			_stopPrice = buyStop;
			_takePrice = buyTake;
			_firstTarget = buyFirstTarget;
			BuyLimit(buyLevel, LotVolume);
		}
		else if (sellLevel != 0 && candle.HighPrice >= sellLevel && Position >= 0)
		{
			_isLong = false;
			_stopPrice = sellStop;
			_takePrice = sellTake;
			_firstTarget = sellFirstTarget;
			SellLimit(sellLevel, LotVolume);
		}
	}

	private decimal GetBuyLevels(out decimal stop, out decimal take, out decimal firstTarget)
	{
		switch (TargetVariant)
		{
			case 1:
				stop = _s2;
				take = _r1;
				firstTarget = _r1;
				return _s1;
			case 2:
				stop = _s2;
				take = _r2;
				firstTarget = _r1;
				return _s1;
			case 3:
				stop = _s3;
				take = _r1;
				firstTarget = _r1;
				return _s2;
			case 4:
				stop = _s3;
				take = _r2;
				firstTarget = _r1;
				return _s2;
			case 5:
				stop = _s3;
				take = _r3;
				firstTarget = _r1;
				return _s2;
			default:
				stop = take = firstTarget = 0;
				return 0;
		}
	}

	private decimal GetSellLevels(out decimal stop, out decimal take, out decimal firstTarget)
	{
		switch (TargetVariant)
		{
			case 1:
				stop = _r2;
				take = _s1;
				firstTarget = _s1;
				return _r1;
			case 2:
				stop = _r2;
				take = _s2;
				firstTarget = _s1;
				return _r1;
			case 3:
				stop = _r3;
				take = _s1;
				firstTarget = _s1;
				return _r2;
			case 4:
				stop = _r3;
				take = _s2;
				firstTarget = _s1;
				return _r2;
			case 5:
				stop = _r3;
				take = _s3;
				firstTarget = _s1;
				return _r2;
			default:
				stop = take = firstTarget = 0;
				return 0;
		}
	}

	private void CheckExit(ICandleMessage candle)
	{
		if (_isLong)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
			if (candle.HighPrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
			if (ModifyStopLoss && _stopPrice < _firstTarget && candle.HighPrice >= _firstTarget)
				_stopPrice = _firstTarget;
		}
		else
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
			if (candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
			if (ModifyStopLoss && _stopPrice > _firstTarget && candle.LowPrice <= _firstTarget)
				_stopPrice = _firstTarget;
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}
}

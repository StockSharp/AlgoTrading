using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adam and Eve strategy.
/// Uses Heiken Ashi candles and a stack of SMAs to detect strong trends.
/// A bearish Heiken Ashi candle without an upper wick and falling averages opens a short.
/// A bullish candle without a lower wick and rising averages opens a long.
/// Each trade targets one ATR from entry without a stop loss.
/// </summary>
public class AdamAndEveStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;
	private decimal? _prevHaHigh;
	private decimal? _prevHaLow;

	private decimal? _sma5Prev1;
	private decimal? _sma5Prev2;
	private decimal? _sma7Prev1;
	private decimal? _sma7Prev2;
	private decimal? _sma9Prev1;
	private decimal? _sma9Prev2;
	private decimal? _sma10Prev1;
	private decimal? _sma10Prev2;
	private decimal? _sma12Prev1;
	private decimal? _sma12Prev2;
	private decimal? _sma14Prev1;
	private decimal? _sma14Prev2;
	private decimal? _sma20Prev1;
	private decimal? _sma20Prev2;

	private decimal? _targetPrice;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// ATR period for profit target.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AdamAndEveStrategy"/>.
	/// </summary>
	public AdamAndEveStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR based profit target", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = null;
		_prevHaClose = null;
		_prevHaHigh = null;
		_prevHaLow = null;

		_sma5Prev1 = _sma5Prev2 = null;
		_sma7Prev1 = _sma7Prev2 = null;
		_sma9Prev1 = _sma9Prev2 = null;
		_sma10Prev1 = _sma10Prev2 = null;
		_sma12Prev1 = _sma12Prev2 = null;
		_sma14Prev1 = _sma14Prev2 = null;
		_sma20Prev1 = _sma20Prev2 = null;

		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma5 = new SimpleMovingAverage { Length = 5 };
		var sma7 = new SimpleMovingAverage { Length = 7 };
		var sma9 = new SimpleMovingAverage { Length = 9 };
		var sma10 = new SimpleMovingAverage { Length = 10 };
		var sma12 = new SimpleMovingAverage { Length = 12 };
		var sma14 = new SimpleMovingAverage { Length = 14 };
		var sma20 = new SimpleMovingAverage { Length = 20 };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma5, sma7, sma9, sma10, sma12, sma14, sma20, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		decimal sma5,
		decimal sma7,
		decimal sma9,
		decimal sma10,
		decimal sma12,
		decimal sma14,
		decimal sma20,
		decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_prevHaOpen.HasValue)
		{
			ComputeHeikenAshi(candle, out var haOpen, out var haClose, out var haHigh, out var haLow);
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevHaHigh = haHigh;
			_prevHaLow = haLow;

			UpdateSma(sma5, ref _sma5Prev1, ref _sma5Prev2);
			UpdateSma(sma7, ref _sma7Prev1, ref _sma7Prev2);
			UpdateSma(sma9, ref _sma9Prev1, ref _sma9Prev2);
			UpdateSma(sma10, ref _sma10Prev1, ref _sma10Prev2);
			UpdateSma(sma12, ref _sma12Prev1, ref _sma12Prev2);
			UpdateSma(sma14, ref _sma14Prev1, ref _sma14Prev2);
			UpdateSma(sma20, ref _sma20Prev1, ref _sma20Prev2);
			return;
		}

		var bearishPrev = _prevHaClose < _prevHaOpen;
		var bullishPrev = _prevHaClose > _prevHaOpen;
		var noUpperWickPrev = _prevHaOpen == _prevHaHigh;
		var noLowerWickPrev = _prevHaOpen == _prevHaLow;

		var smasDown =
			IsDecreasing(sma5, _sma5Prev1, _sma5Prev2) &&
			IsDecreasing(sma7, _sma7Prev1, _sma7Prev2) &&
			IsDecreasing(sma9, _sma9Prev1, _sma9Prev2) &&
			IsDecreasing(sma10, _sma10Prev1, _sma10Prev2) &&
			IsDecreasing(sma12, _sma12Prev1, _sma12Prev2) &&
			IsDecreasing(sma14, _sma14Prev1, _sma14Prev2) &&
			IsDecreasing(sma20, _sma20Prev1, _sma20Prev2);

		var smasUp =
			IsIncreasing(sma5, _sma5Prev1, _sma5Prev2) &&
			IsIncreasing(sma7, _sma7Prev1, _sma7Prev2) &&
			IsIncreasing(sma9, _sma9Prev1, _sma9Prev2) &&
			IsIncreasing(sma10, _sma10Prev1, _sma10Prev2) &&
			IsIncreasing(sma12, _sma12Prev1, _sma12Prev2) &&
			IsIncreasing(sma14, _sma14Prev1, _sma14Prev2) &&
			IsIncreasing(sma20, _sma20Prev1, _sma20Prev2);

		if (Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			if (bearishPrev && noUpperWickPrev && smasDown)
			{
				SellMarket(Volume);
				_targetPrice = candle.ClosePrice - atr;
			}
			else if (bullishPrev && noLowerWickPrev && smasUp)
			{
				BuyMarket(Volume);
				_targetPrice = candle.ClosePrice + atr;
			}
		}
		else if (Position > 0 && _targetPrice.HasValue && candle.HighPrice >= _targetPrice.Value)
		{
			SellMarket(Position);
			_targetPrice = null;
		}
		else if (Position < 0 && _targetPrice.HasValue && candle.LowPrice <= _targetPrice.Value)
		{
			BuyMarket(-Position);
			_targetPrice = null;
		}

		ComputeHeikenAshi(candle, out var nextOpen, out var nextClose, out var nextHigh, out var nextLow);
		_prevHaOpen = nextOpen;
		_prevHaClose = nextClose;
		_prevHaHigh = nextHigh;
		_prevHaLow = nextLow;

		UpdateSma(sma5, ref _sma5Prev1, ref _sma5Prev2);
		UpdateSma(sma7, ref _sma7Prev1, ref _sma7Prev2);
		UpdateSma(sma9, ref _sma9Prev1, ref _sma9Prev2);
		UpdateSma(sma10, ref _sma10Prev1, ref _sma10Prev2);
		UpdateSma(sma12, ref _sma12Prev1, ref _sma12Prev2);
		UpdateSma(sma14, ref _sma14Prev1, ref _sma14Prev2);
		UpdateSma(sma20, ref _sma20Prev1, ref _sma20Prev2);
	}

	private void ComputeHeikenAshi(ICandleMessage candle, out decimal haOpen, out decimal haClose, out decimal haHigh, out decimal haLow)
	{
		haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		haOpen = _prevHaOpen.HasValue ? (_prevHaOpen.Value + _prevHaClose.Value) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
	}

	private static void UpdateSma(decimal current, ref decimal? prev1, ref decimal? prev2)
	{
		prev2 = prev1;
		prev1 = current;
	}

	private static bool IsDecreasing(decimal current, decimal? prev1, decimal? prev2)
		=> prev1.HasValue && prev2.HasValue && current < prev1.Value && prev1.Value < prev2.Value;

	private static bool IsIncreasing(decimal current, decimal? prev1, decimal? prev2)
		=> prev1.HasValue && prev2.HasValue && current > prev1.Value && prev1.Value > prev2.Value;
}

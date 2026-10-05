using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA trend Heikin Ashi entry strategy.
/// Bollinger Bands are calculated on Heikin Ashi closes. After at least two bearish Heikin Ashi candles touching the lower band,
/// a bullish candle closing above the lower band goes long when the fast EMA is above the slow EMA on the higher timeframe; the
/// short side mirrors it at the upper band. The stop is the signal candle's low (high). Half of the position is taken at 1R,
/// after which the stop trails the previous candle's low (high).
/// </summary>
public class EmaTrendHeikinAshiEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe;

	private BollingerBands _bollinger;
	private decimal? _haOpen;
	private decimal? _haClose;
	private int _bearTouchCount;
	private int _bullTouchCount;
	private decimal? _higherFast;
	private decimal? _higherSlow;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private bool _targetDone;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Fast EMA period on the higher timeframe.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period on the higher timeframe.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
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
	/// Higher timeframe for the EMA trend filter.
	/// </summary>
	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaTrendHeikinAshiEntryStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period on the higher timeframe", "Trend");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period on the higher timeframe", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromMinutes(180).TimeFrame())
			.SetDisplay("Higher Timeframe", "Timeframe of the EMA trend filter", "Trend");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_haOpen = null;
		_haClose = null;
		_bearTouchCount = 0;
		_bullTouchCount = 0;
		_higherFast = null;
		_higherSlow = null;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_targetDone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeCandles(HigherTimeframe)
			.Bind(fastEma, slowEma, ProcessHigherCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherFast = fast;
		_higherSlow = slow;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal po && _haClose is decimal pc ? (po + pc) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
		_haOpen = haOpen;
		_haClose = haClose;

		var bandsValue = _bollinger.Process(new DecimalIndicatorValue(_bollinger, haClose, candle.OpenTime) { IsFinal = true });

		if (!_bollinger.IsFormed || bandsValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		var bearTouches = _bearTouchCount;
		var bullTouches = _bullTouchCount;
		var bullish = haClose > haOpen;
		var bearish = haClose < haOpen;
		_bearTouchCount = bearish && haLow <= lower ? _bearTouchCount + 1 : 0;
		_bullTouchCount = bullish && haHigh >= upper ? _bullTouchCount + 1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			ManagePosition(candle);
			return;
		}

		if (_higherFast is not decimal fast || _higherSlow is not decimal slow)
			return;

		var close = candle.ClosePrice;

		if (bearTouches >= 2 && bullish && haClose > lower && fast > slow)
		{
			var risk = close - candle.LowPrice;
			if (risk <= 0m)
				return;

			_stopPrice = candle.LowPrice;
			_targetPrice = close + risk;
			_targetDone = false;
			BuyMarket(Volume);
		}
		else if (bullTouches >= 2 && bearish && haClose < upper && fast < slow)
		{
			var risk = candle.HighPrice - close;
			if (risk <= 0m)
				return;

			_stopPrice = candle.HighPrice;
			_targetPrice = close - risk;
			_targetDone = false;
			SellMarket(Volume);
		}
	}

	private decimal HalfVolume()
	{
		var step = Security?.VolumeStep ?? 0m;
		var half = Math.Abs(Position) / 2m;

		if (step > 0m)
			half = Math.Floor(half / step) * step;

		return half;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				return;
			}

			if (!_targetDone && candle.HighPrice >= _targetPrice)
			{
				_targetDone = true;
				var half = HalfVolume();
				SellMarket(half > 0m ? half : Position);
			}

			if (_targetDone)
				_stopPrice = Math.Max(_stopPrice, candle.LowPrice);
		}
		else
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				return;
			}

			if (!_targetDone && candle.LowPrice <= _targetPrice)
			{
				_targetDone = true;
				var half = HalfVolume();
				BuyMarket(half > 0m ? half : -Position);
			}

			if (_targetDone)
				_stopPrice = Math.Min(_stopPrice, candle.HighPrice);
		}
	}
}

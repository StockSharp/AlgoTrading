using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alliheik Trader: uses smoothed Heiken Ashi candles (EMA of OHLC)
/// with Alligator jaw (long SMA) as trend filter.
/// Entry on HA color change above/below jaw, exit on reversal.
/// </summary>
public class AlliheikTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<int> _jawPeriod;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _prevBullish;
	private bool _hasPrev;
	private decimal _entryPrice;

	public AlliheikTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_smoothPeriod = Param(nameof(SmoothPeriod), 6)
			.SetDisplay("Smooth Period", "EMA period for HA smoothing.", "Indicators");

		_jawPeriod = Param(nameof(JawPeriod), 144)
			.SetDisplay("Jaw Period", "SMA period for Alligator jaw.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = 0;
		_prevHaClose = 0;
		_prevBullish = false;
		_hasPrev = false;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHaOpen = 0;
		_prevHaClose = 0;
		_prevBullish = false;
		_hasPrev = false;
		_entryPrice = 0;

		var ema = new ExponentialMovingAverage { Length = SmoothPeriod };
		var jaw = new SimpleMovingAverage { Length = JawPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, jaw, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal jawVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Compute smoothed Heiken Ashi
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + close) / 4m;
		var haOpen = _prevHaOpen == 0
			? (candle.OpenPrice + close) / 2m
			: (_prevHaOpen + _prevHaClose) / 2m;

		var bullish = haClose > haOpen;

		if (!_hasPrev)
		{
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevBullish = bullish;
			_hasPrev = true;
			return;
		}

		var colorChange = bullish != _prevBullish;

		// Exit on color change
		if (Position > 0 && colorChange && !bullish)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && colorChange && bullish)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Entry on color change confirmed by jaw filter
		if (Position == 0 && colorChange)
		{
			if (bullish && close > jawVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (!bullish && close < jawVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevBullish = bullish;
	}
}

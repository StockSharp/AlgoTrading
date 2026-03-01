using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chandelier Exit strategy with EMA trend filter.
/// Uses ATR-based trailing stops with EMA for trend direction.
/// </summary>
public class ChandelierExitWith200EmaFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _emaLength;

	private bool _initialized;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _prevClose;
	private int _dir = 1;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public ChandelierExitWith200EmaFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend filter", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		_initialized = false;
		_dir = 1;
		_highs.Clear();
		_lows.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, (candle, emaVal, atrVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Track highs and lows for lookback
				_highs.Add(candle.HighPrice);
				_lows.Add(candle.LowPrice);
				if (_highs.Count > AtrPeriod)
					_highs.RemoveAt(0);
				if (_lows.Count > AtrPeriod)
					_lows.RemoveAt(0);

				if (_highs.Count < AtrPeriod)
					return;

				// Calculate highest high and lowest low
				var highest = decimal.MinValue;
				var lowest = decimal.MaxValue;
				foreach (var h in _highs)
					if (h > highest) highest = h;
				foreach (var l in _lows)
					if (l < lowest) lowest = l;

				var longStop = highest - atrVal * AtrMultiplier;
				var shortStop = lowest + atrVal * AtrMultiplier;

				if (!_initialized)
				{
					_longStop = longStop;
					_shortStop = shortStop;
					_prevClose = candle.ClosePrice;
					_initialized = true;
					return;
				}

				var longStopPrev = _longStop;
				var shortStopPrev = _shortStop;

				if (_prevClose > longStopPrev)
					longStop = Math.Max(longStop, longStopPrev);

				if (_prevClose < shortStopPrev)
					shortStop = Math.Min(shortStop, shortStopPrev);

				var prevDir = _dir;
				if (candle.ClosePrice > shortStopPrev)
					_dir = 1;
				else if (candle.ClosePrice < longStopPrev)
					_dir = -1;

				var buySignal = _dir == 1 && prevDir == -1;
				var sellSignal = _dir == -1 && prevDir == 1;

				_longStop = longStop;
				_shortStop = shortStop;
				_prevClose = candle.ClosePrice;

				if (buySignal)
				{
					if (emaVal < candle.ClosePrice)
						BuyMarket(Volume + Math.Max(-Position, 0));
					else if (Position < 0)
						BuyMarket(-Position);
				}
				else if (sellSignal)
				{
					if (emaVal > candle.ClosePrice)
						SellMarket(Volume + Math.Max(Position, 0));
					else if (Position > 0)
						SellMarket(Position);
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}

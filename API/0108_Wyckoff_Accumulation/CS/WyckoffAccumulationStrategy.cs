using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Wyckoff Accumulation pattern.
/// Detects a selling climax followed by sideways accumulation and a spring,
/// then enters long on the markup phase.
/// </summary>
public class WyckoffAccumulationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _sidewaysThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;
	private Highest _highest;
	private Lowest _lowest;

	private enum WyckoffPhase
	{
		None,
		Accumulation,
		Spring,
		Markup
	}

	private WyckoffPhase _phase;
	private decimal _rangeHigh;
	private decimal _rangeLow;
	private int _narrowCount;
	private decimal _entryPrice;
	private decimal _prevMa;
	private decimal _prevClose;
	private int _cooldown;

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Highest/Lowest period for range detection.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// Number of narrow-range candles to confirm accumulation.
	/// </summary>
	public int SidewaysThreshold
	{
		get => _sidewaysThreshold.Value;
		set => _sidewaysThreshold.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public WyckoffAccumulationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "SMA period", "Indicators")
			.SetRange(10, 50);

		_rangePeriod = Param(nameof(RangePeriod), 20)
			.SetDisplay("Range Period", "Highest/Lowest period", "Indicators")
			.SetRange(10, 50);

		_sidewaysThreshold = Param(nameof(SidewaysThreshold), 3)
			.SetDisplay("Sideways Threshold", "Narrow candles to confirm accumulation", "Logic")
			.SetRange(2, 10);

		_cooldownBars = Param(nameof(CooldownBars), 65)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 500);
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
		_ma = default;
		_highest = default;
		_lowest = default;
		_phase = WyckoffPhase.None;
		_rangeHigh = 0;
		_rangeLow = 0;
		_narrowCount = 0;
		_entryPrice = 0;
		_prevMa = 0;
		_prevClose = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };
		_highest = new Highest { Length = RangePeriod };
		_lowest = new Lowest { Length = RangePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, _highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var range = highest - lowest;

		if (range <= 0)
		{
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMa = ma;
			_prevClose = close;
			// Still update phase tracking for exit logic
			if (Position > 0)
			{
				// Exit: price crosses below MA
				if (close < ma && _prevClose >= _prevMa)
				{
					SellMarket();
					_phase = WyckoffPhase.None;
					_narrowCount = 0;
					_cooldown = CooldownBars;
				}
			}
			else if (Position < 0)
			{
				// Exit short: price crosses above MA
				if (close > ma && _prevClose <= _prevMa)
				{
					BuyMarket();
					_phase = WyckoffPhase.None;
					_narrowCount = 0;
					_cooldown = CooldownBars;
				}
			}
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		var candleRange = candle.HighPrice - candle.LowPrice;
		var isNarrow = candleRange < range * 0.4m;
		var isBullish = close > candle.OpenPrice;
		var isBearish = close < candle.OpenPrice;

		// Wyckoff accumulation detection (simplified)
		switch (_phase)
		{
			case WyckoffPhase.None:
				// Look for price near or below the range low (selling climax area)
				if (close <= lowest + range * 0.2m && isBearish)
				{
					_phase = WyckoffPhase.Accumulation;
					_rangeLow = lowest;
					_rangeHigh = highest;
					_narrowCount = 0;
				}
				// Also detect distribution (price near range high for shorts)
				else if (close >= highest - range * 0.2m && isBullish)
				{
					_phase = WyckoffPhase.Accumulation;
					_rangeLow = lowest;
					_rangeHigh = highest;
					_narrowCount = -1; // negative to track distribution
				}
				break;

			case WyckoffPhase.Accumulation:
				if (_narrowCount >= 0)
				{
					// Accumulation (long setup): count narrow-range candles
					if (isNarrow)
						_narrowCount++;

					if (_narrowCount >= SidewaysThreshold)
					{
						_phase = WyckoffPhase.Spring;
					}
					// Reset if price breaks significantly above range
					else if (close > _rangeHigh + range * 0.1m)
					{
						_phase = WyckoffPhase.None;
						_narrowCount = 0;
					}
				}
				else
				{
					// Distribution (short setup): count narrow-range candles
					if (isNarrow)
						_narrowCount--;

					if (_narrowCount <= -SidewaysThreshold)
					{
						_phase = WyckoffPhase.Spring;
					}
					// Reset if price breaks significantly below range
					else if (close < _rangeLow - range * 0.1m)
					{
						_phase = WyckoffPhase.None;
						_narrowCount = 0;
					}
				}
				break;

			case WyckoffPhase.Spring:
				if (_narrowCount > 0)
				{
					// Long spring: price dips below range low then closes back above
					if (candle.LowPrice < _rangeLow && close > _rangeLow)
					{
						_phase = WyckoffPhase.Markup;
					}
					// Or: bullish candle near support with close above MA
					else if (isBullish && close > ma && close > _rangeLow)
					{
						_phase = WyckoffPhase.Markup;
					}
				}
				else
				{
					// Short spring (upthrust): price spikes above range high then closes back below
					if (candle.HighPrice > _rangeHigh && close < _rangeHigh)
					{
						_phase = WyckoffPhase.Markup;
					}
					// Or: bearish candle near resistance with close below MA
					else if (isBearish && close < ma && close < _rangeHigh)
					{
						_phase = WyckoffPhase.Markup;
					}
				}
				break;

			case WyckoffPhase.Markup:
				if (Position == 0)
				{
					if (_narrowCount > 0)
					{
						// Enter long on markup
						if (isBullish && close > ma)
						{
							BuyMarket();
							_entryPrice = close;
							_cooldown = CooldownBars;
							_phase = WyckoffPhase.None;
							_narrowCount = 0;
						}
					}
					else
					{
						// Enter short on markdown
						if (isBearish && close < ma)
						{
							SellMarket();
							_entryPrice = close;
							_cooldown = CooldownBars;
							_phase = WyckoffPhase.None;
							_narrowCount = 0;
						}
					}
				}
				break;
		}

		// Exit logic for open positions
		if (Position > 0)
		{
			// Exit long: price crosses below MA
			if (close < ma && _prevClose >= _prevMa)
			{
				SellMarket();
				_phase = WyckoffPhase.None;
				_narrowCount = 0;
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short: price crosses above MA
			if (close > ma && _prevClose <= _prevMa)
			{
				BuyMarket();
				_phase = WyckoffPhase.None;
				_narrowCount = 0;
				_cooldown = CooldownBars;
			}
		}

		_prevMa = ma;
		_prevClose = close;
	}
}

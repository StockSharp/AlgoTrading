using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Wyckoff Distribution pattern.
/// Detects narrowing ranges near extremes (distribution/accumulation),
/// then enters on upthrust/spring confirmation with MA filter.
/// Uses bar-based cooldown to control trade frequency.
/// </summary>
public class WyckoffDistributionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevMa;
	private decimal _prevClose;
	private int _narrowCount;
	private int _barsSinceEntry;
	private decimal _entryPrice;
	private int _holdBars;

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
	/// Highest/Lowest period.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
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
	public WyckoffDistributionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "SMA period", "Indicators")
			.SetRange(10, 50);

		_rangePeriod = Param(nameof(RangePeriod), 20)
			.SetDisplay("Range Period", "Highest/Lowest period", "Indicators")
			.SetRange(10, 50);

		_cooldownBars = Param(nameof(CooldownBars), 800)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 2000);
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
		_prevMa = 0;
		_prevClose = 0;
		_narrowCount = 0;
		_barsSinceEntry = 0;
		_entryPrice = 0;
		_holdBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsSinceEntry = CooldownBars; // allow immediate first trade

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

		if (range <= 0 || _prevMa == 0)
		{
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		_barsSinceEntry++;

		var candleRange = candle.HighPrice - candle.LowPrice;
		var isNarrow = candleRange < range * 0.35m;

		// Track consecutive narrow-range candles
		if (isNarrow)
			_narrowCount++;
		else
			_narrowCount = 0;

		// Exit logic: hold for minimum bars, then exit on MA cross
		if (Position != 0 && _holdBars > 0)
		{
			_holdBars--;
		}

		if (Position > 0 && _holdBars == 0)
		{
			if (close < ma)
			{
				SellMarket();
				_barsSinceEntry = 0;
			}
		}
		else if (Position < 0 && _holdBars == 0)
		{
			if (close > ma)
			{
				BuyMarket();
				_barsSinceEntry = 0;
			}
		}

		// Entry logic: only when no position and sufficient cooldown
		if (Position == 0 && _barsSinceEntry >= CooldownBars && _narrowCount >= 2)
		{
			var nearTop = close > lowest + range * 0.55m;
			var nearBottom = close < highest - range * 0.55m;

			// Upthrust (short): price near top after consolidation, bearish candle below MA
			if (nearTop && close < candle.OpenPrice && close < ma)
			{
				SellMarket();
				_entryPrice = close;
				_barsSinceEntry = 0;
				_narrowCount = 0;
				_holdBars = 20;
			}
			// Spring (long): price near bottom after consolidation, bullish candle above MA
			else if (nearBottom && close > candle.OpenPrice && close > ma)
			{
				BuyMarket();
				_entryPrice = close;
				_barsSinceEntry = 0;
				_narrowCount = 0;
				_holdBars = 20;
			}
		}

		_prevMa = ma;
		_prevClose = close;
	}
}

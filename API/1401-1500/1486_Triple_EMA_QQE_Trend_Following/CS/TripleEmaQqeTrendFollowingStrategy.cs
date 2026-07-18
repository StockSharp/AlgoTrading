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
/// Triple EMA with QQE-inspired RSI filter and trailing stop.
/// Uses fast/slow EMA crossover confirmed by RSI momentum direction.
/// </summary>
public class TripleEmaQqeTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _trailPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevRsi;
	private decimal _entryPrice;
	private int _cooldown;
	private int _candleCount;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public decimal TrailPct { get => _trailPct.Value; set => _trailPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TripleEmaQqeTrendFollowingStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_trailPct = Param(nameof(TrailPct), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing stop percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_prevRsi = 0;
		_entryPrice = 0;
		_cooldown = 0;
		_candleCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candleCount++;

		if (_prevFast == 0 || _prevSlow == 0 || _prevRsi == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_prevRsi = rsiVal;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_prevRsi = rsiVal;
			return;
		}

		var price = candle.ClosePrice;
		var trail = TrailPct / 100m;

		// EMA trend direction
		var trendUp = fastVal > slowVal;
		var trendDown = fastVal < slowVal;

		// RSI crosses
		var rsiCrossUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiCrossDown = _prevRsi >= 50m && rsiVal < 50m;

		// Exit conditions
		if (Position > 0)
		{
			if (price < _entryPrice * (1m - trail) || rsiCrossDown)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 80;
			}
		}
		else if (Position < 0)
		{
			if (price > _entryPrice * (1m + trail) || rsiCrossUp)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 80;
			}
		}

		// Entry: RSI cross + EMA trend confirmation
		if (Position == 0)
		{
			if (rsiCrossUp && trendUp)
			{
				BuyMarket();
				_entryPrice = price;
				_cooldown = 80;
			}
			else if (rsiCrossDown && trendDown)
			{
				SellMarket();
				_entryPrice = price;
				_cooldown = 80;
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
		_prevRsi = rsiVal;
	}
}

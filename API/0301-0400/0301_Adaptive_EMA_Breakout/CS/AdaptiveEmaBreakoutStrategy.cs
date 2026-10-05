using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive EMA (Kaufman) breakout with trend confirmation.
/// Buys when price closes above a rising adaptive EMA and sells when it closes below a falling one.
/// Positions are reversed on the opposite signal and protected by an ATR-multiple stop.
/// </summary>
public class AdaptiveEmaBreakoutStrategy : Strategy
{
	private const int _atrPeriod = 14;

	private readonly StrategyParam<int> _fast;
	private readonly StrategyParam<int> _slow;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAdaptiveEma;
	private decimal _stopPrice;

	/// <summary>
	/// Fast smoothing period of the adaptive EMA.
	/// </summary>
	public int Fast
	{
		get => _fast.Value;
		set => _fast.Value = value;
	}

	/// <summary>
	/// Slow smoothing period of the adaptive EMA.
	/// </summary>
	public int Slow
	{
		get => _slow.Value;
		set => _slow.Value = value;
	}

	/// <summary>
	/// Efficiency ratio lookback of the adaptive EMA.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
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
	/// Initialize <see cref="AdaptiveEmaBreakoutStrategy"/>.
	/// </summary>
	public AdaptiveEmaBreakoutStrategy()
	{
		_fast = Param(nameof(Fast), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast smoothing period of the adaptive EMA", "Indicators");

		_slow = Param(nameof(Slow), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow smoothing period of the adaptive EMA", "Indicators");

		_lookback = Param(nameof(Lookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Efficiency ratio lookback of the adaptive EMA", "Indicators");

		_stopMultiplier = Param(nameof(StopMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevAdaptiveEma = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adaptiveEma = new KaufmanAdaptiveMovingAverage
		{
			Length = Lookback,
			FastSCPeriod = Fast,
			SlowSCPeriod = Slow,
		};
		var atr = new AverageTrueRange { Length = _atrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(adaptiveEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adaptiveEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adaptiveEmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevAdaptiveEma = _prevAdaptiveEma;
		_prevAdaptiveEma = adaptiveEmaValue;

		if (prevAdaptiveEma is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStop(candle))
			return;

		var close = candle.ClosePrice;
		var stopDistance = StopMultiplier * atrValue;

		if (close > adaptiveEmaValue && adaptiveEmaValue > prev && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close - stopDistance : 0m;
		}
		else if (close < adaptiveEmaValue && adaptiveEmaValue < prev && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close + stopDistance : 0m;
		}
	}

	private bool CheckStop(ICandleMessage candle)
	{
		if (_stopPrice == 0m)
			return false;

		if ((Position > 0 && candle.LowPrice <= _stopPrice) || (Position < 0 && candle.HighPrice >= _stopPrice))
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(-Position);

			_stopPrice = 0m;
			return true;
		}

		return false;
	}
}

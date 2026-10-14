using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flex ATR strategy.
/// EMA, RSI and ATR periods are chosen from the candle timeframe: up to 5 minutes 8/21 EMAs, RSI 9 and ATR 10; up to 30 minutes
/// 12/26, RSI 14, ATR 14; up to 4 hours 20/50, RSI 14, ATR 14; longer 50/200, RSI 14, ATR 20. A fast EMA crossing above the slow
/// one with RSI above 50 goes long and the opposite cross with RSI below 50 goes short, reversing an opposite position. A position
/// closes at a stop AtrStopMult ATRs away or a target AtrProfitMult ATRs away, both set at entry; with EnableTrailingStop the stop
/// also follows the best close at AtrTrailMult ATRs.
/// </summary>
public class FlexAtrStrategy : Strategy
{
	private const decimal _rsiMiddle = 50m;

	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<decimal> _atrProfitMult;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _atrTrailMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Stop distance in ATRs.
	/// </summary>
	public decimal AtrStopMult
	{
		get => _atrStopMult.Value;
		set => _atrStopMult.Value = value;
	}

	/// <summary>
	/// Target distance in ATRs.
	/// </summary>
	public decimal AtrProfitMult
	{
		get => _atrProfitMult.Value;
		set => _atrProfitMult.Value = value;
	}

	/// <summary>
	/// Let the stop follow the best close.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing distance in ATRs.
	/// </summary>
	public decimal AtrTrailMult
	{
		get => _atrTrailMult.Value;
		set => _atrTrailMult.Value = value;
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
	/// Constructor.
	/// </summary>
	public FlexAtrStrategy()
	{
		_atrStopMult = Param(nameof(AtrStopMult), 3m)
			.SetNotNegative()
			.SetDisplay("ATR Stop Mult", "Stop distance in ATRs", "Risk");

		_atrProfitMult = Param(nameof(AtrProfitMult), 1.5m)
			.SetNotNegative()
			.SetDisplay("ATR Profit Mult", "Target distance in ATRs", "Risk");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Trailing Stop", "Let the stop follow the best close", "Risk");

		_atrTrailMult = Param(nameof(AtrTrailMult), 1m)
			.SetNotNegative()
			.SetDisplay("ATR Trail Mult", "Trailing distance in ATRs", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var (fastLength, slowLength, rsiLength, atrLength) = SelectPeriods(CandleType.Arg is TimeSpan frame ? frame : TimeSpan.Zero);

		var fast = new ExponentialMovingAverage { Length = fastLength };
		var slow = new ExponentialMovingAverage { Length = slowLength };
		var rsi = new RelativeStrengthIndex { Length = rsiLength };
		var atr = new AverageTrueRange { Length = atrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private static (int fast, int slow, int rsi, int atr) SelectPeriods(TimeSpan frame)
	{
		if (frame <= TimeSpan.FromMinutes(5))
			return (8, 21, 9, 10);

		if (frame <= TimeSpan.FromMinutes(30))
			return (12, 26, 14, 14);

		if (frame <= TimeSpan.FromHours(4))
			return (20, 50, 14, 14);

		return (50, 200, 14, 20);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal lastFast || prevSlow is not decimal lastSlow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var crossUp = lastFast <= lastSlow && fast > slow;
		var crossDown = lastFast >= lastSlow && fast < slow;

		if (crossUp && rsi > _rsiMiddle && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrStopMult > 0 ? close - AtrStopMult * atr : 0m;
			_targetPrice = AtrProfitMult > 0 ? close + AtrProfitMult * atr : 0m;
			return;
		}

		if (crossDown && rsi < _rsiMiddle && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrStopMult > 0 ? close + AtrStopMult * atr : 0m;
			_targetPrice = AtrProfitMult > 0 ? close - AtrProfitMult * atr : 0m;
			return;
		}

		if (Position > 0)
		{
			if ((_stopPrice > 0 && candle.LowPrice <= _stopPrice) || (_targetPrice > 0 && candle.HighPrice >= _targetPrice))
			{
				SellMarket(Position);
				return;
			}

			if (EnableTrailingStop)
			{
				var trail = close - AtrTrailMult * atr;
				if (trail > _stopPrice)
					_stopPrice = trail;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice > 0 && candle.HighPrice >= _stopPrice) || (_targetPrice > 0 && candle.LowPrice <= _targetPrice))
			{
				BuyMarket(-Position);
				return;
			}

			if (EnableTrailingStop)
			{
				var trail = close + AtrTrailMult * atr;
				if (_stopPrice <= 0 || trail < _stopPrice)
					_stopPrice = trail;
			}
		}
	}
}

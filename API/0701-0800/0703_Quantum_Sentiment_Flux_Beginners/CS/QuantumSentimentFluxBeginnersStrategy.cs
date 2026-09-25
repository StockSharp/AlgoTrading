using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quantum Sentiment Flux (Beginners).
/// Trades EMA crosses only when the separation is strong enough relative to ATR,
/// and manages the position with ATR-based stop/target distances.
/// </summary>
public class QuantumSentimentFluxBeginnersStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _maStrengthThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _quantity;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private int _cooldownRemaining;
	private int _armedDirection;

	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal MaStrengthThreshold { get => _maStrengthThreshold.Value; set => _maStrengthThreshold.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal Quantity { get => _quantity.Value; set => _quantity.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public QuantumSentimentFluxBeginnersStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length.", "Indicators");
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length.", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length used for strength and protection.", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple used for the stop distance.", "Risk");
		_maStrengthThreshold = Param(nameof(MaStrengthThreshold), 0.25m)
			.SetNotNegative()
			.SetDisplay("MA Strength Threshold", "Minimum EMA separation expressed as ATR multiples.", "Signals");
		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Finished bars to wait after a completed trade before a new entry.", "Signals");
		_quantity = Param(nameof(Quantity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Quantity", "Order volume for entries.", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_cooldownRemaining = 0;
		_armedDirection = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0m || _prevSlow == 0m)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// Protection is evaluated regardless of the entry cooldown.
		if (Position > 0 && _entryPrice > 0m && _entryAtr > 0m)
		{
			var stopDistance = _entryAtr * AtrMultiplier;
			if (candle.LowPrice <= _entryPrice - stopDistance ||
				candle.HighPrice >= _entryPrice + stopDistance * 2m)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				_cooldownRemaining = CooldownBars;
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0m && _entryAtr > 0m)
		{
			var stopDistance = _entryAtr * AtrMultiplier;
			if (candle.HighPrice >= _entryPrice + stopDistance ||
				candle.LowPrice <= _entryPrice - stopDistance * 2m)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				_cooldownRemaining = CooldownBars;
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp)
			_armedDirection = 1;
		else if (crossDown)
			_armedDirection = -1;

		if ((_armedDirection > 0 && fast <= slow) || (_armedDirection < 0 && fast >= slow))
			_armedDirection = 0;

		var strongEnough = atr > 0m && Math.Abs(fast - slow) >= atr * MaStrengthThreshold;

		if (_cooldownRemaining == 0 && Position == 0 && strongEnough && _armedDirection != 0)
		{
			if (_armedDirection > 0)
			{
				BuyMarket(Quantity);
				_entryPrice = candle.ClosePrice;
				_entryAtr = atr;
			}
			else
			{
				SellMarket(Quantity);
				_entryPrice = candle.ClosePrice;
				_entryAtr = atr;
			}

			_armedDirection = 0;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_entryAtr = 0m;
	}
}

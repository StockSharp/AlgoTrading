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
/// Strategy that simulates a straddle approach: defines upper/lower breakout levels
/// from a consolidation range (ATR-based) and enters on breakouts with trailing stop.
/// </summary>
public class StraddleTrailStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossMult;
	private readonly StrategyParam<decimal> _takeProfitMult;
	private readonly StrategyParam<decimal> _trailMult;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal? _stopLevel;
	private decimal? _takeLevel;
	private int _barsSinceEntry;
	private int _cooldownCounter;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLossMult { get => _stopLossMult.Value; set => _stopLossMult.Value = value; }
	public decimal TakeProfitMult { get => _takeProfitMult.Value; set => _takeProfitMult.Value = value; }
	public decimal TrailMult { get => _trailMult.Value; set => _trailMult.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StraddleTrailStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation length", "ATR");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Breakout distance multiplier", "ATR");

		_stopLossMult = Param(nameof(StopLossMult), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "Stop loss as ATR multiple", "Risk");

		_takeProfitMult = Param(nameof(TakeProfitMult), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP Multiplier", "Take profit as ATR multiple", "Risk");

		_trailMult = Param(nameof(TrailMult), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Multiplier", "Trailing distance as ATR multiple", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown", "Bars to wait after exit", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle subscription", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_stopLevel = null;
		_takeLevel = null;
		_barsSinceEntry = 0;
		_cooldownCounter = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var sma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Manage existing position
		if (Position != 0)
		{
			_barsSinceEntry++;

			if (Position > 0)
			{
				// Trail stop up
				var newTrail = close - TrailMult * atr;
				if (_stopLevel == null || newTrail > _stopLevel)
					_stopLevel = newTrail;

				// Check stop or take
				if (close <= _stopLevel || (_takeLevel != null && close >= _takeLevel))
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
			else
			{
				// Trail stop down
				var newTrail = close + TrailMult * atr;
				if (_stopLevel == null || newTrail < _stopLevel)
					_stopLevel = newTrail;

				// Check stop or take
				if (close >= _stopLevel || (_takeLevel != null && close <= _takeLevel))
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}

			return;
		}

		// Cooldown after exit
		if (_cooldownCounter > 0)
		{
			_cooldownCounter--;
			return;
		}

		// Entry: breakout above/below SMA + ATR distance
		var upperLevel = sma + AtrMultiplier * atr;
		var lowerLevel = sma - AtrMultiplier * atr;

		if (close > upperLevel)
		{
			BuyMarket();
			_entryPrice = close;
			_stopLevel = close - StopLossMult * atr;
			_takeLevel = close + TakeProfitMult * atr;
			_barsSinceEntry = 0;
		}
		else if (close < lowerLevel)
		{
			SellMarket();
			_entryPrice = close;
			_stopLevel = close + StopLossMult * atr;
			_takeLevel = close - TakeProfitMult * atr;
			_barsSinceEntry = 0;
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0;
		_stopLevel = null;
		_takeLevel = null;
		_barsSinceEntry = 0;
		_cooldownCounter = CooldownBars;
	}
}

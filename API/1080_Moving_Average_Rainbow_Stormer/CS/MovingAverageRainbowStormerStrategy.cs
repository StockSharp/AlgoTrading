using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Rainbow (Stormer) strategy.
/// Uses multiple EMA rainbow for trend confirmation.
/// Enters long when price is above all MAs (bullish alignment), short when below.
/// </summary>
public class MovingAverageRainbowStormerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetFactor;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minTrendSpreadPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ma3;
	private EMA _ma8;
	private EMA _ma20;
	private EMA _ma50;
	private decimal _entryPrice;
	private bool _prevBullish;
	private bool _prevBearish;
	private int _barIndex;
	private int _lastSignalBar = int.MinValue;

	public decimal TargetFactor { get => _targetFactor.Value; set => _targetFactor.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal MinTrendSpreadPercent { get => _minTrendSpreadPercent.Value; set => _minTrendSpreadPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageRainbowStormerStrategy()
	{
		_targetFactor = Param(nameof(TargetFactor), 2m);
		_cooldownBars = Param(nameof(CooldownBars), 12).SetGreaterThanZero();
		_minTrendSpreadPercent = Param(nameof(MinTrendSpreadPercent), 0.03m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevBullish = false;
		_prevBearish = false;
		_barIndex = 0;
		_lastSignalBar = int.MinValue;

		_ma3 = new EMA { Length = 3 };
		_ma8 = new EMA { Length = 8 };
		_ma20 = new EMA { Length = 20 };
		_ma50 = new EMA { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma3, _ma8, _ma20, _ma50, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma3, decimal ma8, decimal ma20, decimal ma50)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var bullishAlignment = ma3 > ma8 && ma8 > ma20 && ma20 > ma50;
		var bearishAlignment = ma3 < ma8 && ma8 < ma20 && ma20 < ma50;
		var trendSpreadPercent = close != 0m ? Math.Abs(ma3 - ma50) / close * 100m : 0m;
		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;
		var bullishSignal = bullishAlignment && trendSpreadPercent >= MinTrendSpreadPercent;
		var bearishSignal = bearishAlignment && trendSpreadPercent >= MinTrendSpreadPercent;

		if (canSignal && bullishSignal && close > ma3 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_lastSignalBar = _barIndex;
		}
		else if (canSignal && bearishSignal && close < ma3 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_lastSignalBar = _barIndex;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var risk = _entryPrice - ma20;
			if (risk > 0)
			{
				var target = _entryPrice + risk * TargetFactor;
				if (close >= target || close < ma20)
				{
					SellMarket();
					_lastSignalBar = _barIndex;
				}
			}
			else if (close < ma8)
			{
				SellMarket();
				_lastSignalBar = _barIndex;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var risk = ma20 - _entryPrice;
			if (risk > 0)
			{
				var target = _entryPrice - risk * TargetFactor;
				if (close <= target || close > ma20)
				{
					BuyMarket();
					_lastSignalBar = _barIndex;
				}
			}
			else if (close > ma8)
			{
				BuyMarket();
				_lastSignalBar = _barIndex;
			}
		}

		_prevBullish = bullishAlignment;
		_prevBearish = bearishAlignment;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_prevBullish = false;
		_prevBearish = false;
		_barIndex = 0;
		_lastSignalBar = int.MinValue;
	}
}

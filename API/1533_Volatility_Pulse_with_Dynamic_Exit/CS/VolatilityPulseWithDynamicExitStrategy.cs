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
/// Strategy using volatility expansion with momentum confirmation and dynamic exits.
/// Uses StdDev as volatility proxy and manual momentum calculation.
/// Enters on vol expansion + momentum, exits on TP/SL or time-based exit.
/// </summary>
public class VolatilityPulseWithDynamicExitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _volThreshold;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _stopPct;

	private readonly List<decimal> _closes = new();
	private int _barIndex;
	private int _entryBarIndex;
	private decimal _entryPrice;
	private decimal _stopDist;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public decimal VolThreshold { get => _volThreshold.Value; set => _volThreshold.Value = value; }
	public int ExitBars { get => _exitBars.Value; set => _exitBars.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }

	public VolatilityPulseWithDynamicExitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_stdLength = Param(nameof(StdLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Volatility period", "Parameters");

		_momentumLength = Param(nameof(MomentumLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum lookback", "Parameters");

		_volThreshold = Param(nameof(VolThreshold), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Vol Threshold", "StdDev expansion multiplier", "Parameters");

		_exitBars = Param(nameof(ExitBars), 42)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Time-based exit after N bars", "Risk");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "TP to SL ratio", "Risk");

		_stopPct = Param(nameof(StopPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_barIndex = 0;
		_entryBarIndex = -1;
		_entryPrice = 0;
		_stopDist = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = StdLength };
		var sma = new SimpleMovingAverage { Length = StdLength };

		_closes.Clear();
		_barIndex = 0;
		_entryBarIndex = -1;
		_entryPrice = 0;
		_stopDist = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdVal, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		_closes.Add(close);

		while (_closes.Count > MomentumLength + 1)
			_closes.RemoveAt(0);

		_barIndex++;

		if (_closes.Count <= MomentumLength || stdVal <= 0 || smaVal <= 0)
			return;

		// Momentum = current close - close N bars ago
		var momentum = close - _closes[0];

		// Volatility expansion: stdDev relative to price vs average
		var volRatio = stdVal / smaVal;
		var volExpansion = volRatio > VolThreshold * 0.01m;

		var momentumUp = momentum > 0;
		var momentumDown = momentum < 0;

		// TP/SL management
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			var sl = _entryPrice - _stopDist;
			var tp = _entryPrice + _stopDist * RiskReward;

			if (close <= sl || close >= tp)
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_entryBarIndex = -1;
			}
			// Time-based exit
			else if (_entryBarIndex >= 0 && _barIndex - _entryBarIndex >= ExitBars)
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_entryBarIndex = -1;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			var sl = _entryPrice + _stopDist;
			var tp = _entryPrice - _stopDist * RiskReward;

			if (close >= sl || close <= tp)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_entryBarIndex = -1;
			}
			// Time-based exit
			else if (_entryBarIndex >= 0 && _barIndex - _entryBarIndex >= ExitBars)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_entryBarIndex = -1;
			}
		}

		// Entry signals
		if (Position <= 0 && volExpansion && momentumUp)
		{
			BuyMarket();
			_entryPrice = close;
			_stopDist = close * StopPct / 100m;
			_entryBarIndex = _barIndex;
		}
		else if (Position >= 0 && volExpansion && momentumDown)
		{
			SellMarket();
			_entryPrice = close;
			_stopDist = close * StopPct / 100m;
			_entryBarIndex = _barIndex;
		}
	}
}

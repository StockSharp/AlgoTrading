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
/// Strategy based on MACD and VWAP indicators.
/// Enters long when MACD > Signal and price > VWAP
/// Enters short when MACD < Signal and price < VWAP
/// </summary>
public class MacdVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private int _cooldown;
	private bool _hasPrevDiff;
	private decimal _prevDiff;

	/// <summary>
	/// MACD fast period
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public MacdVwapStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
			
			.SetOptimize(8, 16, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
			
			.SetOptimize(20, 30, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")
			
			.SetOptimize(7, 12, 1);

		_cooldownBars = Param(nameof(CooldownBars), 35)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between new entries", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		_cooldown = 0;
		_hasPrevDiff = false;
		_prevDiff = 0m;
	}

		/// <inheritdoc />
		protected override void OnStarted2(DateTime time)
		{
		base.OnStarted2(time);

		// Create indicators

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var vwap = new VolumeWeightedMovingAverage();

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, vwap, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue vwapValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get additional values from MACD (signal line)
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd ?? 0m;
		var signal = macdTyped.Signal ?? 0m;
		var vwap = vwapValue.ToDecimal();

		// Current price (close of the candle)
		var price = candle.ClosePrice;
		var diff = macd - signal;

		if (!_hasPrevDiff)
		{
			_hasPrevDiff = true;
			_prevDiff = diff;
			return;
		}

		var crossUp = _prevDiff <= 0m && diff > 0m;
		var crossDown = _prevDiff >= 0m && diff < 0m;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDiff = diff;
			return;
		}

		// Trading logic
		if (crossUp && price > vwap * 1.001m && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (crossDown && price < vwap * 0.999m && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (crossDown && Position > 0)
		{
			SellMarket(Position);
			_cooldown = CooldownBars;
		}
		else if (crossUp && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}

		_prevDiff = diff;
	}
}

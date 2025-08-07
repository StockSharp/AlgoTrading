namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD Long Strategy
/// </summary>
public class MacdLongStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<decimal> _longStopLossPercent;
	private readonly StrategyParam<decimal> _longTakeProfitPercent;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _shortStopLossPercent;
	private readonly StrategyParam<decimal> _shortTakeProfitPercent;
	private readonly StrategyParam<int> _rsiOverSold;
	private readonly StrategyParam<int> _rsiOverBought;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _lookbackBars;

	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergenceSignal _macd;
	
	private int _barsSinceOversold;
	private int _barsSinceOverbought;

	public MacdLongStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// Long strategy parameters
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Enable long strategy", "Long Strategy");
		_longStopLossPercent = Param(nameof(LongStopLossPercent), 50m)
			.SetDisplay("Long Stop Loss %", "Stop loss percentage", "Long Strategy");
		_longTakeProfitPercent = Param(nameof(LongTakeProfitPercent), 50m)
			.SetDisplay("Long Take Profit %", "Take profit percentage", "Long Strategy");

		// Short strategy parameters
		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Enable short strategy", "Short Strategy");
		_shortStopLossPercent = Param(nameof(ShortStopLossPercent), 50m)
			.SetDisplay("Short Stop Loss %", "Stop loss percentage", "Short Strategy");
		_shortTakeProfitPercent = Param(nameof(ShortTakeProfitPercent), 50m)
			.SetDisplay("Short Take Profit %", "Take profit percentage", "Short Strategy");

		// RSI parameters
		_rsiOverSold = Param(nameof(RsiOverSold), 30)
			.SetDisplay("RSI Oversold", "Oversold level", "RSI");
		_rsiOverBought = Param(nameof(RsiOverBought), 70)
			.SetDisplay("RSI Overbought", "Overbought level", "RSI");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		// MACD parameters
		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast Length", "Fast MA period", "MACD");
		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow Length", "Slow MA period", "MACD");
		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal Length", "Signal period", "MACD");

		// Strategy
		_lookbackBars = Param(nameof(LookbackBars), 10)
			.SetDisplay("Lookback Bars", "Bars to check for RSI conditions", "Strategy");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	public decimal LongStopLossPercent
	{
		get => _longStopLossPercent.Value;
		set => _longStopLossPercent.Value = value;
	}

	public decimal LongTakeProfitPercent
	{
		get => _longTakeProfitPercent.Value;
		set => _longTakeProfitPercent.Value = value;
	}

	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public decimal ShortStopLossPercent
	{
		get => _shortStopLossPercent.Value;
		set => _shortStopLossPercent.Value = value;
	}

	public decimal ShortTakeProfitPercent
	{
		get => _shortTakeProfitPercent.Value;
		set => _shortTakeProfitPercent.Value = value;
	}

	public int RsiOverSold
	{
		get => _rsiOverSold.Value;
		set => _rsiOverSold.Value = value;
	}

	public int RsiOverBought
	{
		get => _rsiOverBought.Value;
		set => _rsiOverBought.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength },
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, _macd, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Enable protection
		if (EnableLong || EnableShort)
		{
			var tp = EnableLong ? new Unit(LongTakeProfitPercent, UnitTypes.Percent) : 
					 EnableShort ? new Unit(ShortTakeProfitPercent, UnitTypes.Percent) : null;
			var sl = EnableLong ? new Unit(LongStopLossPercent, UnitTypes.Percent) : 
					 EnableShort ? new Unit(ShortStopLossPercent, UnitTypes.Percent) : null;
			
			if (tp != null || sl != null)
				StartProtection(tp, sl);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_rsi.IsFormed || !_macd.IsFormed)
			return;

		var rsiPrice = rsiValue.ToDecimal();

		// Update RSI condition trackers
		if (rsiPrice <= RsiOverSold)
		{
			_barsSinceOversold = 0;
		}
		else
		{
			_barsSinceOversold++;
		}

		if (rsiPrice >= RsiOverBought)
		{
			_barsSinceOverbought = 0;
		}
		else
		{
			_barsSinceOverbought++;
		}

		// Check if RSI was oversold/overbought within lookback period
		var wasOversold = _barsSinceOversold <= LookbackBars;
		var wasOverbought = _barsSinceOverbought <= LookbackBars;

		// Get MACD values from complex indicator
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine)
			return;

		if (macdTyped.Signal is not decimal signalLine)
			return;

		// Get previous MACD values for crossover detection
		var prevMacdValue = _macd.GetValue<MovingAverageConvergenceDivergenceSignalValue>(1);

		if (prevMacdValue.Macd is not decimal prevMacdLine)
			return;

		if (prevMacdValue.Signal is not decimal prevSignalLine)
			return;

		// Detect crossovers
		var crossoverBull = macdLine > signalLine && prevMacdLine <= prevSignalLine;
		var crossoverBear = macdLine < signalLine && prevMacdLine >= prevSignalLine;

		// Strategy signals
		var buySignal = wasOversold && crossoverBull;
		var sellSignal = wasOverbought && crossoverBear;

		// Execute trades
		if (EnableLong && buySignal)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (EnableLong && sellSignal && Position > 0)
		{
			ClosePosition();
		}

		if (EnableShort && sellSignal)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		else if (EnableShort && buySignal && Position < 0)
		{
			ClosePosition();
		}
	}
}
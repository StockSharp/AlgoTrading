namespace StockSharp.Samples.Strategies;

using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

/// <summary>
/// MACD + BB + RSI Strategy
/// </summary>
public class MacdBbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _macdUseEma;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useDmiFilter;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<int> _adxKeyLevel;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<bool> _useSL;
	private readonly StrategyParam<Unit> _stopValue;
	private readonly StrategyParam<bool> _useTP;
	private readonly StrategyParam<Unit> _takeValue;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private DirectionalIndex _dmi;

	private int _barsInPosition;
	private decimal? _entryPrice;

	public MacdBbRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// MACD parameters
		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast Length", "Fast period", "MACD");
		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow Length", "Slow period", "MACD");
		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("Signal Smoothing", "Signal period", "MACD");
		_macdUseEma = Param(nameof(MacdUseEma), true)
			.SetDisplay("Use EMA", "Use EMA for MACD", "MACD");

		// Bollinger Bands parameters
		_bbLength = Param(nameof(BBLength), 20)
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");
		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		// RSI parameters
		_rsiLength = Param(nameof(RSILength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		// DMI parameters
		_useDmiFilter = Param(nameof(UseDmiFilter), false)
			.SetDisplay("Use DMI Filter", "Enable DMI filter", "Directional Movement Index");
		_dmiLength = Param(nameof(DmiLength), 14)
			.SetDisplay("DI Length", "DMI period", "Directional Movement Index");
		_adxSmoothing = Param(nameof(AdxSmoothing), 13)
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "Directional Movement Index");
		_adxKeyLevel = Param(nameof(AdxKeyLevel), 23)
			.SetDisplay("ADX Key Level", "ADX threshold", "Directional Movement Index");

		// Strategy parameters
		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");
		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");
		_closeAfterXBars = Param(nameof(CloseAfterXBars), false)
			.SetDisplay("Close after X bars", "Close position after X bars", "Strategy");
		_xBars = Param(nameof(XBars), 12)
			.SetDisplay("# bars", "Number of bars", "Strategy");

		// Risk management
		_useSL = Param(nameof(UseSL), false)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");
		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss value", "Stop Loss");
		_useTP = Param(nameof(UseTP), false)
			.SetDisplay("Enable TP", "Enable Take Profit", "Take Profit");
		_takeValue = Param(nameof(TakeValue), new Unit(1, UnitTypes.Percent))
			.SetDisplay("Take Profit", "Take profit value", "Take Profit");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

	public bool MacdUseEma
	{
		get => _macdUseEma.Value;
		set => _macdUseEma.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public bool UseDmiFilter
	{
		get => _useDmiFilter.Value;
		set => _useDmiFilter.Value = value;
	}

	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	public int AdxSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	public int AdxKeyLevel
	{
		get => _adxKeyLevel.Value;
		set => _adxKeyLevel.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public bool CloseAfterXBars
	{
		get => _closeAfterXBars.Value;
		set => _closeAfterXBars.Value = value;
	}

	public int XBars
	{
		get => _xBars.Value;
		set => _xBars.Value = value;
	}

	public bool UseSL
	{
		get => _useSL.Value;
		set => _useSL.Value = value;
	}

	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	public bool UseTP
	{
		get => _useTP.Value;
		set => _useTP.Value = value;
	}

	public Unit TakeValue
	{
		get => _takeValue.Value;
		set => _takeValue.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_barsInPosition = default;
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			}
		};

		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RSILength
		};

		if (UseDmiFilter)
		{
			_dmi = new DirectionalIndex
			{
				Length = DmiLength
			};
		}

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);

		if (UseDmiFilter)
		{
			subscription
				.BindEx(_macd, _bollinger, _rsi, _dmi, OnProcessWithDmi)
				.Start();
		}
		else
		{
			subscription
				.BindEx(_macd, _bollinger, _rsi, OnProcessWithoutDmi)
				.Start();
		}

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		// Enable protection
		if (UseSL && UseTP)
		{
			StartProtection(TakeValue, StopValue);
		}
		else if (UseSL)
		{
			StartProtection(new(), StopValue);
		}
		else if (UseTP)
		{
			StartProtection(TakeValue, new());
		}
	}

	private void OnProcessWithDmi(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue dmiValue)
	{
		ProcessCandle(candle, macdValue, bollingerValue, rsiValue.ToDecimal(), dmiValue);
	}

	private void OnProcessWithoutDmi(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		ProcessCandle(candle, macdValue, bollingerValue, rsiValue.ToDecimal(), null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bollingerValue, decimal rsiValue, IIndicatorValue dmiValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_macd.IsFormed || !_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		if (UseDmiFilter && !_dmi.IsFormed)
			return;

		// Get indicator values
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;

		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		var upper = bollingerTyped.UpBand;
		var lower = bollingerTyped.LowBand;
		var basis = bollingerTyped.MovingAverage;

		// Get previous MACD values for crossover detection
		var prevMacdTyped = _macd.GetValue<MovingAverageConvergenceDivergenceSignalValue>(1);
		var prevMacdLine = prevMacdTyped.Macd;
		var prevSignalLine = prevMacdTyped.Signal;

		// DMI filter
		var dmiFilter = true;
		if (UseDmiFilter && dmiValue != null)
		{
			var dmiTyped = (DirectionalIndexValue)dmiValue;

			if (dmiTyped.Plus is not decimal diPlus ||
				dmiTyped.Minus is not decimal diMinus)
			{
				return; // Skip if DMI values are not available
			}

			// Long filter: DI+ > ADX key level
			// Short filter: DI- > ADX key level
			dmiFilter = Position <= 0 ? diPlus > AdxKeyLevel : diMinus > AdxKeyLevel;
		}

		// Entry conditions
		var macdCrossover = macdLine > signalLine && prevMacdLine <= prevSignalLine;
		var macdCrossunder = macdLine < signalLine && prevMacdLine >= prevSignalLine;

		var entryLong = macdCrossover && 
						rsiValue < 50 && 
						candle.ClosePrice < basis && 
						dmiFilter;

		var entryShort = macdCrossunder && 
						 rsiValue > 50 && 
						 candle.ClosePrice > basis && 
						 dmiFilter;

		// Exit conditions
		var exitLong = rsiValue > 70 || candle.ClosePrice > upper;
		var exitShort = rsiValue < 30 || candle.ClosePrice < lower;

		// Track bars in position
		if (Position != 0)
		{
			_barsInPosition++;
		}
		else
		{
			_barsInPosition = 0;
			_entryPrice = null;
		}

		// Close after X bars if enabled
		if (CloseAfterXBars && _barsInPosition >= XBars && _entryPrice.HasValue)
		{
			if (Position > 0 && candle.ClosePrice > _entryPrice.Value)
			{
				exitLong = true;
			}
			else if (Position < 0 && candle.ClosePrice < _entryPrice.Value)
			{
				exitShort = true;
			}
		}

		// Execute trades
		if (ShowLong && exitLong && Position > 0)
		{
			ClosePosition();
		}
		else if (ShowShort && exitShort && Position < 0)
		{
			ClosePosition();
		}
		else if (ShowLong && entryLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_barsInPosition = 0;
		}
		else if (ShowShort && entryShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_barsInPosition = 0;
		}
	}
}
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modular range-trading strategy using Bollinger Bands, RSI, MACD, ATR and ADX.
/// Implements two independent mean-reversion logics with mutual exclusion.
/// </summary>
public class ModularRangeTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiMaPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThresholdLogic1;
	private readonly StrategyParam<decimal> _atrMultiplierLogic1;
	private readonly StrategyParam<bool> _useBbExitLogic1;
	private readonly StrategyParam<bool> _useRsiExitLogic1;
	private readonly StrategyParam<int> _rsiObLogic2;
	private readonly StrategyParam<int> _rsiOsLogic2;
	private readonly StrategyParam<decimal> _adxThresholdLogic2;
	private readonly StrategyParam<decimal> _atrMultiplierLogic2;
	private readonly StrategyParam<bool> _useBbExitLogic2;
	private readonly StrategyParam<bool> _useRsiExitLogic2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevClose;
	private decimal _prevBbUpper;
	private decimal _prevBbLower;
	private decimal _prevBbMiddle;
	private decimal _prevRsi;
	private decimal _prevRsiMa;
	private decimal _entryPrice;
	private bool _isLogic1Active;
	private bool _isLogic2Active;
	private bool _isLong;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width (standard deviations).
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI moving average period.
	/// </summary>
	public int RsiMaPeriod
	{
		get => _rsiMaPeriod.Value;
		set => _rsiMaPeriod.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for logic 1.
	/// </summary>
	public decimal AdxThresholdLogic1
	{
		get => _adxThresholdLogic1.Value;
		set => _adxThresholdLogic1.Value = value;
	}

	/// <summary>
	/// ATR multiplier for logic 1 stops.
	/// </summary>
	public decimal AtrMultiplierLogic1
	{
		get => _atrMultiplierLogic1.Value;
		set => _atrMultiplierLogic1.Value = value;
	}

	/// <summary>
	/// Enable Bollinger Band exit for logic 1.
	/// </summary>
	public bool UseBbExitLogic1
	{
		get => _useBbExitLogic1.Value;
		set => _useBbExitLogic1.Value = value;
	}

	/// <summary>
	/// Enable RSI exit for logic 1.
	/// </summary>
	public bool UseRsiExitLogic1
	{
		get => _useRsiExitLogic1.Value;
		set => _useRsiExitLogic1.Value = value;
	}

	/// <summary>
	/// RSI overbought level for logic 2.
	/// </summary>
	public int RsiObLogic2
	{
		get => _rsiObLogic2.Value;
		set => _rsiObLogic2.Value = value;
	}

	/// <summary>
	/// RSI oversold level for logic 2.
	/// </summary>
	public int RsiOsLogic2
	{
		get => _rsiOsLogic2.Value;
		set => _rsiOsLogic2.Value = value;
	}

	/// <summary>
	/// ADX threshold for logic 2.
	/// </summary>
	public decimal AdxThresholdLogic2
	{
		get => _adxThresholdLogic2.Value;
		set => _adxThresholdLogic2.Value = value;
	}

	/// <summary>
	/// ATR multiplier for logic 2 stops.
	/// </summary>
	public decimal AtrMultiplierLogic2
	{
		get => _atrMultiplierLogic2.Value;
		set => _atrMultiplierLogic2.Value = value;
	}

	/// <summary>
	/// Enable Bollinger Band exit for logic 2.
	/// </summary>
	public bool UseBbExitLogic2
	{
		get => _useBbExitLogic2.Value;
		set => _useBbExitLogic2.Value = value;
	}

	/// <summary>
	/// Enable RSI exit for logic 2.
	/// </summary>
	public bool UseRsiExitLogic2
	{
		get => _useRsiExitLogic2.Value;
		set => _useRsiExitLogic2.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ModularRangeTradingStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands width", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_rsiMaPeriod = Param(nameof(RsiMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI MA Period", "RSI moving average period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "MACD");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "ATR");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX period", "ADX");

		_adxThresholdLogic1 = Param(nameof(AdxThresholdLogic1), 40m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold L1", "ADX threshold for logic 1", "Logic1");

		_atrMultiplierLogic1 = Param(nameof(AtrMultiplierLogic1), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mult L1", "ATR multiplier for logic 1", "Logic1");

		_useBbExitLogic1 = Param(nameof(UseBbExitLogic1), true)
			.SetDisplay("Use BB Exit L1", "Use Bollinger exit for logic 1", "Logic1");

		_useRsiExitLogic1 = Param(nameof(UseRsiExitLogic1), true)
			.SetDisplay("Use RSI Exit L1", "Use RSI exit for logic 1", "Logic1");

		_rsiObLogic2 = Param(nameof(RsiObLogic2), 80)
			.SetGreaterThanZero()
			.SetDisplay("RSI OB L2", "RSI overbought for logic 2", "Logic2");

		_rsiOsLogic2 = Param(nameof(RsiOsLogic2), 30)
			.SetGreaterThanZero()
			.SetDisplay("RSI OS L2", "RSI oversold for logic 2", "Logic2");

		_adxThresholdLogic2 = Param(nameof(AdxThresholdLogic2), 35m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold L2", "ADX threshold for logic 2", "Logic2");

		_atrMultiplierLogic2 = Param(nameof(AtrMultiplierLogic2), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mult L2", "ATR multiplier for logic 2", "Logic2");

		_useBbExitLogic2 = Param(nameof(UseBbExitLogic2), true)
			.SetDisplay("Use BB Exit L2", "Use Bollinger exit for logic 2", "Logic2");

		_useRsiExitLogic2 = Param(nameof(UseRsiExitLogic2), true)
			.SetDisplay("Use RSI Exit L2", "Use RSI exit for logic 2", "Logic2");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevClose = 0m;
		_prevBbUpper = 0m;
		_prevBbLower = 0m;
		_prevBbMiddle = 0m;
		_prevRsi = 0m;
		_prevRsiMa = 0m;
		_entryPrice = 0m;
		_isLogic1Active = false;
		_isLogic2Active = false;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var rsi = new RSI { Length = RsiPeriod };
		var rsiMa = new SMA { Length = RsiMaPeriod };

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bollinger, rsi, rsiMa, macd, atr, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue rsiMaValue, IIndicatorValue macdValue, IIndicatorValue atrValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;

		var rsi = rsiValue.ToDecimal();
		var rsiMa = rsiMaValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var atr = atrValue.ToDecimal();
		var adx = adxValue.ToDecimal();

		var isRanging1 = adx < AdxThresholdLogic1;
		var isRanging2 = adx < AdxThresholdLogic2;

		var macdCrossUp = _prevMacd <= _prevSignal && macd > signal;
		var macdCrossDown = _prevMacd >= _prevSignal && macd < signal;
		var priceCrossBackAboveLower = _prevClose <= _prevBbLower && candle.ClosePrice > lower;
		var priceCrossBackBelowUpper = _prevClose >= _prevBbUpper && candle.ClosePrice < upper;
		var rsiCrossUnder = _prevRsi >= _prevRsiMa && rsi < rsiMa;
		var rsiCrossOver = _prevRsi <= _prevRsiMa && rsi > rsiMa;

		var priceBelowMiddle = candle.ClosePrice < middle;
		var priceAboveMiddle = candle.ClosePrice > middle;

		var logic1Long = !_isLogic2Active && isRanging1 && macdCrossUp && rsi > rsiMa && priceBelowMiddle;
		var logic1Short = !_isLogic2Active && isRanging1 && macdCrossDown && rsi < rsiMa && priceAboveMiddle;
		var logic2Long = !_isLogic1Active && isRanging2 && priceCrossBackAboveLower && rsi <= RsiOsLogic2;
		var logic2Short = !_isLogic1Active && isRanging2 && priceCrossBackBelowUpper && rsi >= RsiObLogic2;

		if (logic1Long && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = true;
			_isLogic1Active = true;
			_isLogic2Active = false;
		}
		else if (logic1Short && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = false;
			_isLogic1Active = true;
			_isLogic2Active = false;
		}
		else if (logic2Long && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = true;
			_isLogic1Active = false;
			_isLogic2Active = true;
		}
		else if (logic2Short && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = false;
			_isLogic1Active = false;
			_isLogic2Active = true;
		}

		if (Position > 0)
		{
			var stop = candle.ClosePrice - atr * (_isLogic1Active ? AtrMultiplierLogic1 : AtrMultiplierLogic2);
			if (candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_isLogic1Active = false;
				_isLogic2Active = false;
			}
			else if (_isLogic1Active && UseBbExitLogic1 && _prevClose <= _prevBbUpper && candle.ClosePrice > upper)
			{
				SellMarket(Position);
				_isLogic1Active = false;
			}
			else if (_isLogic2Active && UseBbExitLogic2 && _prevClose <= _prevBbMiddle && candle.ClosePrice > middle)
			{
				SellMarket(Position);
				_isLogic2Active = false;
			}
			else if (_isLogic1Active && UseRsiExitLogic1 && rsiCrossUnder && priceAboveMiddle)
			{
				SellMarket(Position);
				_isLogic1Active = false;
			}
			else if (_isLogic2Active && UseRsiExitLogic2 && rsiCrossUnder && priceAboveMiddle)
			{
				SellMarket(Position);
				_isLogic2Active = false;
			}
		}
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + atr * (_isLogic1Active ? AtrMultiplierLogic1 : AtrMultiplierLogic2);
			if (candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				_isLogic1Active = false;
				_isLogic2Active = false;
			}
			else if (_isLogic1Active && UseBbExitLogic1 && _prevClose >= _prevBbLower && candle.ClosePrice < lower)
			{
				BuyMarket(-Position);
				_isLogic1Active = false;
			}
			else if (_isLogic2Active && UseBbExitLogic2 && _prevClose >= _prevBbMiddle && candle.ClosePrice < middle)
			{
				BuyMarket(-Position);
				_isLogic2Active = false;
			}
			else if (_isLogic1Active && UseRsiExitLogic1 && rsiCrossOver && priceBelowMiddle)
			{
				BuyMarket(-Position);
				_isLogic1Active = false;
			}
			else if (_isLogic2Active && UseRsiExitLogic2 && rsiCrossOver && priceBelowMiddle)
			{
				BuyMarket(-Position);
				_isLogic2Active = false;
			}
		}

		if (Position == 0)
		{
			_isLogic1Active = false;
			_isLogic2Active = false;
		}

		_prevMacd = macd;
		_prevSignal = signal;
		_prevClose = candle.ClosePrice;
		_prevBbUpper = upper;
		_prevBbLower = lower;
		_prevBbMiddle = middle;
		_prevRsi = rsi;
		_prevRsiMa = rsiMa;
	}
}

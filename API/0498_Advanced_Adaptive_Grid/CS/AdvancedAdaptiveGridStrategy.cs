using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced Adaptive Grid Trading Strategy.
/// </summary>
public class AdvancedAdaptiveGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseGridSize;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _useVolatilityGrid;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _superLongMaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<decimal> _maxLossPerDay;
	private readonly StrategyParam<bool> _timeBasedExit;
	private readonly StrategyParam<int> _maxHoldingPeriod;

	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _shortMa;
	private SimpleMovingAverage _longMa;
	private SimpleMovingAverage _superLongMa;
	private MovingAverageConvergenceDivergence _macd;
	private Momentum _momentum;

	private readonly List<decimal> _gridLevels = new();
	private int _positionCount;
	private bool _inTrade;
	private decimal _trailingStopLevel;
	private decimal _lastEntryPrice;
	private DateTimeOffset _entryTime;
	private DateTime _lastResetDay;
	private decimal _dailyPnL;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base grid size in percent.
	/// </summary>
	public decimal BaseGridSize
	{
		get => _baseGridSize.Value;
		set => _baseGridSize.Value = value;
	}

	/// <summary>
	/// Maximum number of positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Use ATR based grid sizing.
	/// </summary>
	public bool UseVolatilityGrid
	{
		get => _useVolatilityGrid.Value;
		set => _useVolatilityGrid.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for grid sizing.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Short moving average length.
	/// </summary>
	public int ShortMaLength
	{
		get => _shortMaLength.Value;
		set => _shortMaLength.Value = value;
	}

	/// <summary>
	/// Long moving average length.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// Super long moving average length.
	/// </summary>
	public int SuperLongMaLength
	{
		get => _superLongMaLength.Value;
		set => _superLongMaLength.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Maximum daily loss percent.
	/// </summary>
	public decimal MaxLossPerDay
	{
		get => _maxLossPerDay.Value;
		set => _maxLossPerDay.Value = value;
	}

	/// <summary>
	/// Use time based exit.
	/// </summary>
	public bool TimeBasedExit
	{
		get => _timeBasedExit.Value;
		set => _timeBasedExit.Value = value;
	}

	/// <summary>
	/// Maximum holding period in hours.
	/// </summary>
	public int MaxHoldingPeriod
	{
		get => _maxHoldingPeriod.Value;
		set => _maxHoldingPeriod.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdvancedAdaptiveGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");

		_baseGridSize = Param(nameof(BaseGridSize), 1m)
							.SetGreaterThanZero()
							.SetDisplay("Base Grid Size %", "Base grid step as percentage", "Grid")
							.SetCanOptimize(true)
							.SetOptimize(0.5m, 5m, 0.5m);

		_maxPositions = Param(nameof(MaxPositions), 5)
							.SetGreaterThanZero()
							.SetDisplay("Max Positions", "Maximum number of grid positions", "Grid");

		_useVolatilityGrid =
			Param(nameof(UseVolatilityGrid), true).SetDisplay("Use Volatility Grid", "Use ATR based grid", "Grid");

		_atrLength =
			Param(nameof(AtrLength), 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
							 .SetRange(0.1m, 5m)
							 .SetDisplay("ATR Multiplier", "ATR multiplier for grid", "Grid");

		_rsiLength =
			Param(nameof(RsiLength), 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
							 .SetRange(50, 100)
							 .SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold =
			Param(nameof(RsiOversold), 30).SetRange(0, 50).SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_shortMaLength = Param(nameof(ShortMaLength), 20)
							 .SetGreaterThanZero()
							 .SetDisplay("Short MA", "Short moving average length", "Trend");

		_longMaLength = Param(nameof(LongMaLength), 50)
							.SetGreaterThanZero()
							.SetDisplay("Long MA", "Long moving average length", "Trend");

		_superLongMaLength = Param(nameof(SuperLongMaLength), 200)
								 .SetGreaterThanZero()
								 .SetDisplay("Super Long MA", "Super long moving average length", "Trend");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
							  .SetGreaterThanZero()
							  .SetDisplay("MACD Fast", "MACD fast period", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
							  .SetGreaterThanZero()
							  .SetDisplay("MACD Slow", "MACD slow period", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
								.SetGreaterThanZero()
								.SetDisplay("MACD Signal", "MACD signal period", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
							   .SetGreaterThanZero()
							   .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
								 .SetGreaterThanZero()
								 .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
							   .SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk Management");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1m)
								   .SetGreaterThanZero()
								   .SetDisplay("Trailing Stop %", "Trailing stop percentage", "Risk Management");

		_maxLossPerDay = Param(nameof(MaxLossPerDay), 5m)
							 .SetGreaterThanZero()
							 .SetDisplay("Max Loss Per Day %", "Stop trading after this daily loss", "Risk Management");

		_timeBasedExit = Param(nameof(TimeBasedExit), true)
							 .SetDisplay("Time Based Exit", "Enable time based exit", "Risk Management");

		_maxHoldingPeriod =
			Param(nameof(MaxHoldingPeriod), 48)
				.SetGreaterThanZero()
				.SetDisplay("Max Holding (hours)", "Maximum holding period in hours", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_shortMa = new SimpleMovingAverage { Length = ShortMaLength };
		_longMa = new SimpleMovingAverage { Length = LongMaLength };
		_superLongMa = new SimpleMovingAverage { Length = SuperLongMaLength };
		_macd = new MovingAverageConvergenceDivergence { Fast = MacdFastLength, Slow = MacdSlowLength,
														 Signal = MacdSignalLength };
		_momentum = new Momentum { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, _atr, _shortMa, _longMa, _superLongMa, _macd, _momentum, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue, decimal shortMaValue,
							   decimal longMaValue, decimal superLongMaValue, decimal macdLine, decimal macdSignal,
							   decimal macdHist, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_atr.IsFormed || !_shortMa.IsFormed || !_longMa.IsFormed || !_superLongMa.IsFormed ||
			!_macd.IsFormed || !_momentum.IsFormed)
			return;

		var currentPrice = candle.ClosePrice;

		if (_lastResetDay != candle.ServerTime.Date)
		{
			_dailyPnL = 0m;
			_lastResetDay = candle.ServerTime.Date;
		}

		var normalizedAtr = atrValue / currentPrice * 100m;
		var gridSize = UseVolatilityGrid ? Math.Max(BaseGridSize, normalizedAtr * AtrMultiplier) : BaseGridSize;

		var shortTermBullish = shortMaValue > longMaValue;
		var longTermBullish = longMaValue > superLongMaValue;
		var macdBullish = macdLine > macdSignal && macdLine > 0;
		var rsiBullish = rsiValue < RsiOversold;
		var momentumBullish = momentumValue > 0;

		var shortTermBearish = shortMaValue < longMaValue;
		var longTermBearish = longMaValue < superLongMaValue;
		var macdBearish = macdLine < macdSignal && macdLine < 0;
		var rsiBearish = rsiValue > RsiOverbought;
		var momentumBearish = momentumValue < 0;

		var bullishStrength = (shortTermBullish ? 20 : 0) + (longTermBullish ? 30 : 0) + (macdBullish ? 20 : 0) +
							  (rsiBullish ? 15 : 0) + (momentumBullish ? 15 : 0);
		var bearishStrength = (shortTermBearish ? 20 : 0) + (longTermBearish ? 30 : 0) + (macdBearish ? 20 : 0) +
							  (rsiBearish ? 15 : 0) + (momentumBearish ? 15 : 0);

		var strongBullish = bullishStrength >= 70;
		var moderateBullish = bullishStrength >= 40 && bullishStrength < 70;
		var strongBearish = bearishStrength >= 70;
		var moderateBearish = bearishStrength >= 40 && bearishStrength < 70;
		var sideways = bullishStrength < 40 && bearishStrength < 40;

		_gridLevels.Clear();
		if (strongBearish || moderateBearish)
		{
			for (var i = 1; i <= MaxPositions; i++)
				_gridLevels.Add(currentPrice * (1 + gridSize / 100m * i));
		}
		else if (strongBullish || moderateBullish)
		{
			for (var i = 1; i <= MaxPositions; i++)
				_gridLevels.Add(currentPrice * (1 - gridSize / 100m * i));
		}
		else
		{
			var half = MaxPositions / 2;
			for (var i = 1; i <= half; i++)
				_gridLevels.Add(currentPrice * (1 + gridSize / 100m * i));
			for (var i = 1; i <= half; i++)
				_gridLevels.Add(currentPrice * (1 - gridSize / 100m * i));
		}

		var enterLong = false;
		var enterShort = false;
		var closeLong = false;
		var closeShort = false;

		if (_dailyPnL <= -(Portfolio.CurrentValue * (MaxLossPerDay / 100m)))
		{
			closeLong = Position > 0;
			closeShort = Position < 0;
		}
		else
		{
			if (!_inTrade)
			{
				if ((strongBearish || moderateBearish) && rsiValue > 60 && _positionCount < MaxPositions)
				{
					foreach (var level in _gridLevels)
					{
						if (currentPrice <= level)
						{
							enterShort = true;
							break;
						}
					}
				}
				else if ((strongBullish || moderateBullish) && rsiValue < 40 && _positionCount < MaxPositions)
				{
					foreach (var level in _gridLevels)
					{
						if (currentPrice >= level)
						{
							enterLong = true;
							break;
						}
					}
				}
				else if (sideways)
				{
					if (rsiValue > 70 && Position >= 0 && _positionCount < MaxPositions)
					{
						foreach (var level in _gridLevels)
						{
							if (currentPrice <= level)
							{
								enterShort = true;
								break;
							}
						}
					}
					else if (rsiValue < 30 && Position <= 0 && _positionCount < MaxPositions)
					{
						foreach (var level in _gridLevels)
						{
							if (currentPrice >= level)
							{
								enterLong = true;
								break;
							}
						}
					}
				}
			}
			else
			{
				var stopLossPrice =
					_lastEntryPrice * (Position > 0 ? (1 - StopLossPercent / 100m) : (1 + StopLossPercent / 100m));
				var takeProfitPrice =
					_lastEntryPrice * (Position > 0 ? (1 + TakeProfitPercent / 100m) : (1 - TakeProfitPercent / 100m));

				if ((Position > 0 && currentPrice < stopLossPrice) || (Position < 0 && currentPrice > stopLossPrice))
				{
					closeLong = Position > 0;
					closeShort = Position < 0;
				}
				else if ((Position > 0 && currentPrice > takeProfitPrice) ||
						 (Position < 0 && currentPrice < takeProfitPrice))
				{
					closeLong = Position > 0;
					closeShort = Position < 0;
				}
				else if (UseTrailingStop && ((Position > 0 && currentPrice < _trailingStopLevel) ||
											 (Position < 0 && currentPrice > _trailingStopLevel)))
				{
					closeLong = Position > 0;
					closeShort = Position < 0;
				}
				else if (TimeBasedExit && (candle.ServerTime - _entryTime).TotalHours >= MaxHoldingPeriod)
				{
					closeLong = Position > 0;
					closeShort = Position < 0;
				}
				else if ((Position > 0 && (strongBearish || moderateBearish)) ||
						 (Position < 0 && (strongBullish || moderateBullish)))
				{
					closeLong = Position > 0;
					closeShort = Position < 0;
				}

				if (UseTrailingStop)
				{
					if (Position > 0)
					{
						var level = currentPrice * (1 - TrailingStopPercent / 100m);
						if (level > _trailingStopLevel || _trailingStopLevel == 0)
							_trailingStopLevel = level;
					}
					else if (Position < 0)
					{
						var level = currentPrice * (1 + TrailingStopPercent / 100m);
						if (level < _trailingStopLevel || _trailingStopLevel == 0)
							_trailingStopLevel = level;
					}
				}
			}
		}

		if (enterLong)
		{
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
			_lastEntryPrice = currentPrice;
			_entryTime = candle.ServerTime;
			_trailingStopLevel = currentPrice * (1 - TrailingStopPercent / 100m);
			_inTrade = true;
			_positionCount++;
		}

		if (enterShort)
		{
			RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
			_lastEntryPrice = currentPrice;
			_entryTime = candle.ServerTime;
			_trailingStopLevel = currentPrice * (1 + TrailingStopPercent / 100m);
			_inTrade = true;
			_positionCount++;
		}

		if (closeLong || closeShort)
		{
			if (Position > 0)
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			else if (Position < 0)
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));

			var exitPrice = currentPrice;
			_dailyPnL += Position > 0 ? (exitPrice - _lastEntryPrice) * Volume : (_lastEntryPrice - exitPrice) * Volume;
			_inTrade = false;
			if (_positionCount > 0)
				_positionCount--;
			_trailingStopLevel = 0;
			_lastEntryPrice = 0;
		}
	}
}

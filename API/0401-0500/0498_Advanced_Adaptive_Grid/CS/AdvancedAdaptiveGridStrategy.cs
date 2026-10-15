namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Advanced Adaptive Grid Trading Strategy.
/// Trend direction comes from three moving averages together with MACD, the distance between grid
/// levels adapts to volatility through ATR, and the open grid is guarded by stop-loss, take-profit,
/// trailing stop, a maximum holding period and a daily loss limit.
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

	private int _gridDirection;
	private int _openLevels;
	private decimal _averagePrice;
	private decimal _lastLevelPrice;
	private decimal _bestPrice;
	private int _barsInPosition;
	private DateTime _currentDay;
	private decimal _dayStartPnL;
	private decimal _dayStartValue;
	private bool _dayLossReached;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BaseGridSize { get => _baseGridSize.Value; set => _baseGridSize.Value = value; }
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }
	public bool UseVolatilityGrid { get => _useVolatilityGrid.Value; set => _useVolatilityGrid.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }
	public int SuperLongMaLength { get => _superLongMaLength.Value; set => _superLongMaLength.Value = value; }
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }
	public decimal MaxLossPerDay { get => _maxLossPerDay.Value; set => _maxLossPerDay.Value = value; }
	public bool TimeBasedExit { get => _timeBasedExit.Value; set => _timeBasedExit.Value = value; }
	public int MaxHoldingPeriod { get => _maxHoldingPeriod.Value; set => _maxHoldingPeriod.Value = value; }

	public AdvancedAdaptiveGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_baseGridSize = Param(nameof(BaseGridSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Grid Size", "Distance between grid levels in percent of price", "Grid");

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of grid levels open at the same time", "Grid");

		_useVolatilityGrid = Param(nameof(UseVolatilityGrid), true)
			.SetDisplay("Use Volatility Grid", "Size the grid step from ATR instead of Base Grid Size", "Grid");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for the volatility grid", "Grid");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for the grid step", "Grid");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

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
			.SetDisplay("MACD Fast", "MACD fast moving average length", "Trend");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow moving average length", "Trend");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line length", "Trend");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from the average entry", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage from the average entry", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Protect an open grid with a trailing stop", "Risk");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop %", "Trailing stop distance from the best price", "Risk");

		_maxLossPerDay = Param(nameof(MaxLossPerDay), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss Per Day %", "Daily loss limit in percent of the account value", "Risk");

		_timeBasedExit = Param(nameof(TimeBasedExit), true)
			.SetDisplay("Time Based Exit", "Close the grid after Max Holding Period bars", "Risk");

		_maxHoldingPeriod = Param(nameof(MaxHoldingPeriod), 48)
			.SetGreaterThanZero()
			.SetDisplay("Max Holding Period", "Maximum number of bars a grid stays open", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetGrid();

		_currentDay = default;
		_dayStartPnL = 0;
		_dayStartValue = 0;
		_dayLossReached = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var shortMa = new SimpleMovingAverage { Length = ShortMaLength };
		var longMa = new SimpleMovingAverage { Length = LongMaLength };
		var superLongMa = new SimpleMovingAverage { Length = SuperLongMaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFastLength }, LongMa = { Length = MacdSlowLength } },
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, rsi, shortMa, longMa, superLongMa, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawIndicator(area, superLongMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue rsiValue, IIndicatorValue shortMaValue, IIndicatorValue longMaValue, IIndicatorValue superLongMaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue.IsEmpty || rsiValue.IsEmpty || shortMaValue.IsEmpty || longMaValue.IsEmpty || superLongMaValue.IsEmpty || macdValue.IsEmpty)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal macdSignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var shortMa = shortMaValue.ToDecimal();
		var longMa = longMaValue.ToDecimal();
		var superLongMa = superLongMaValue.ToDecimal();
		var price = candle.ClosePrice;

		UpdateDailyLoss(candle.OpenTime.Date);

		// ATR keeps the distance between levels proportional to current volatility, otherwise the
		// levels sit a fixed percentage of price apart.
		var gridStep = UseVolatilityGrid ? atr * AtrMultiplier : price * BaseGridSize / 100m;

		if (gridStep <= 0)
			return;

		var trendUp = shortMa > longMa && price > superLongMa && macd > macdSignal;
		var trendDown = shortMa < longMa && price < superLongMa && macd < macdSignal;

		if (_gridDirection != 0)
		{
			_barsInPosition++;
			_bestPrice = _gridDirection > 0
				? Math.Max(_bestPrice, candle.HighPrice)
				: Math.Min(_bestPrice, candle.LowPrice);

			if (TryCloseGrid(price, trendUp, trendDown))
				return;
		}

		if (_dayLossReached)
			return;

		if (_gridDirection == 0)
			TryOpenGrid(price, rsi, trendUp, trendDown);
		else
			TryAddLevel(price, rsi, gridStep);
	}

	private void UpdateDailyLoss(DateTime day)
	{
		if (_currentDay != day)
		{
			_currentDay = day;
			_dayStartPnL = PnL;
			_dayStartValue = Portfolio?.CurrentValue ?? 0m;
			_dayLossReached = false;
		}

		// Without a known account value there is nothing to take the percentage of.
		if (_dayStartValue <= 0)
			return;

		if (PnL - _dayStartPnL <= -_dayStartValue * MaxLossPerDay / 100m)
			_dayLossReached = true;
	}

	private bool TryCloseGrid(decimal price, bool trendUp, bool trendDown)
	{
		if (Position == 0)
			return false;

		var isLong = _gridDirection > 0;

		var stopPrice = isLong
			? _averagePrice * (1 - StopLossPercent / 100m)
			: _averagePrice * (1 + StopLossPercent / 100m);

		var takePrice = isLong
			? _averagePrice * (1 + TakeProfitPercent / 100m)
			: _averagePrice * (1 - TakeProfitPercent / 100m);

		var exit = isLong
			? price <= stopPrice || price >= takePrice
			: price >= stopPrice || price <= takePrice;

		// The trailing stop follows the best price reached by the grid and engages only once the
		// grid is at least one trailing distance in profit.
		if (!exit && UseTrailingStop)
		{
			exit = isLong
				? _bestPrice >= _averagePrice * (1 + TrailingStopPercent / 100m) && price <= _bestPrice * (1 - TrailingStopPercent / 100m)
				: _bestPrice <= _averagePrice * (1 - TrailingStopPercent / 100m) && price >= _bestPrice * (1 + TrailingStopPercent / 100m);
		}

		if (!exit && TimeBasedExit && _barsInPosition >= MaxHoldingPeriod)
			exit = true;

		// The trend turned against the whole grid.
		if (!exit && (isLong ? trendDown : trendUp))
			exit = true;

		if (!exit && _dayLossReached)
			exit = true;

		if (!exit)
			return false;

		if (isLong)
			SellMarket(Math.Abs(Position));
		else
			BuyMarket(Math.Abs(Position));

		ResetGrid();

		return true;
	}

	private void TryOpenGrid(decimal price, decimal rsi, bool trendUp, bool trendDown)
	{
		if (Position != 0)
			return;

		// In a trending market the first level follows the trend while RSI is not at the opposite
		// extreme; in a sideways market the grid fades the RSI extremes instead.
		var goLong = trendUp ? rsi < RsiOverbought : !trendDown && rsi < RsiOversold;
		var goShort = trendDown ? rsi > RsiOversold : !trendUp && rsi > RsiOverbought;

		if (goLong)
		{
			BuyMarket(Volume);
			StartGrid(1, price);
		}
		else if (goShort)
		{
			SellMarket(Volume);
			StartGrid(-1, price);
		}
	}

	private void TryAddLevel(decimal price, decimal rsi, decimal gridStep)
	{
		if (_openLevels >= MaxPositions)
			return;

		// The next level is filled once price has travelled a full grid step against the grid.
		if (_gridDirection > 0)
		{
			if (price > _lastLevelPrice - gridStep || rsi >= RsiOverbought)
				return;

			BuyMarket(Volume);
		}
		else
		{
			if (price < _lastLevelPrice + gridStep || rsi <= RsiOversold)
				return;

			SellMarket(Volume);
		}

		AddLevel(price);
	}

	private void StartGrid(int direction, decimal price)
	{
		_gridDirection = direction;
		_openLevels = 1;
		_averagePrice = price;
		_lastLevelPrice = price;
		_bestPrice = price;
		_barsInPosition = 0;
	}

	private void AddLevel(decimal price)
	{
		// Every level has the same volume, so the average entry is the plain mean of the level prices.
		_averagePrice = (_averagePrice * _openLevels + price) / (_openLevels + 1);
		_openLevels++;
		_lastLevelPrice = price;
	}

	private void ResetGrid()
	{
		_gridDirection = 0;
		_openLevels = 0;
		_averagePrice = 0;
		_lastLevelPrice = 0;
		_bestPrice = 0;
		_barsInPosition = 0;
	}
}

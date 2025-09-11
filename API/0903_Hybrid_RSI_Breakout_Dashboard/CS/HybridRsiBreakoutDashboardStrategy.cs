namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Hybrid strategy combining RSI mean reversion and breakout entries with dashboard tracking.
/// </summary>
public class HybridRsiBreakoutDashboardStrategy : Strategy
{
	private enum TradeType
	{
		None,
		Rsi,
		Breakout
	}

	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<decimal> _rsiExit;
	private readonly StrategyParam<int> _breakoutLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;

	private AverageDirectionalIndex _adx;
	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _breakoutStop;
	private TradeType _currentTrade;
	private string _lastTradeType = "None";
	private string _lastDirection = "None";
	private int _barIndex;

	/// <summary>
	/// Initialize <see cref="HybridRsiBreakoutDashboardStrategy"/>.
	/// </summary>
	public HybridRsiBreakoutDashboardStrategy()
	{
		_adxLength = Param(nameof(AdxLength), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("ADX Length", "Length for ADX indicator", "Indicators")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 30, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
							.SetGreaterThanZero()
							.SetDisplay("ADX Threshold", "Trend detection threshold", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(10m, 40m, 5m);

		_emaLength = Param(nameof(EmaLength), 200)
						 .SetGreaterThanZero()
						 .SetDisplay("EMA Length", "Length for EMA trend filter", "Indicators")
						 .SetCanOptimize(true)
						 .SetOptimize(100, 300, 50);

		_rsiLength = Param(nameof(RsiLength), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("RSI Length", "Lookback period for RSI", "Indicators")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 30, 2);

		_rsiBuy = Param(nameof(RsiBuy), 40m).SetDisplay("RSI Buy", "RSI buy threshold", "Strategy Parameters");

		_rsiSell = Param(nameof(RsiSell), 60m).SetDisplay("RSI Sell", "RSI sell threshold", "Strategy Parameters");

		_rsiExit = Param(nameof(RsiExit), 50m).SetDisplay("RSI Exit", "RSI exit threshold", "Strategy Parameters");

		_breakoutLength = Param(nameof(BreakoutLength), 20)
							  .SetGreaterThanZero()
							  .SetDisplay("Breakout Lookback", "Lookback for breakout levels", "Strategy Parameters")
							  .SetCanOptimize(true)
							  .SetOptimize(10, 40, 5);

		_atrLength = Param(nameof(AtrLength), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("ATR Length", "ATR period for trailing stop", "Risk")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 30, 2);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
							 .SetGreaterThanZero()
							 .SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Risk")
							 .SetCanOptimize(true)
							 .SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Timeframe", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero))
						 .SetDisplay("Start Date", "Start date filter", "General");
	}

	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	public decimal RsiBuy
	{
		get => _rsiBuy.Value;
		set => _rsiBuy.Value = value;
	}
	public decimal RsiSell
	{
		get => _rsiSell.Value;
		set => _rsiSell.Value = value;
	}
	public decimal RsiExit
	{
		get => _rsiExit.Value;
		set => _rsiExit.Value = value;
	}
	public int BreakoutLength
	{
		get => _breakoutLength.Value;
		set => _breakoutLength.Value = value;
	}
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHighest = 0m;
		_prevLowest = 0m;
		_breakoutStop = 0m;
		_currentTrade = TradeType.None;
		_lastTradeType = "None";
		_lastDirection = "None";
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_highest = new Highest { Length = BreakoutLength };
		_lowest = new Lowest { Length = BreakoutLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_adx, _ema, _rsi, _atr, _highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adxValue, decimal emaValue, decimal rsiValue,
							   decimal atrValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			return;
		}

		if (!_adx.IsFormed || !_ema.IsFormed || !_rsi.IsFormed || !_atr.IsFormed || !_highest.IsFormed ||
			!_lowest.IsFormed)
		{
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			return;
		}

		if (candle.OpenTime < StartDate)
		{
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			return;
		}

		var isTrending = adxValue > AdxThreshold;
		var bullish = candle.ClosePrice > emaValue;
		var bearish = candle.ClosePrice < emaValue;
		var isRanging = !isTrending;

		var rsiLong = isRanging && rsiValue < RsiBuy && bullish;
		var rsiShort = isRanging && rsiValue > RsiSell && bearish;
		var rsiLongExit = rsiValue > RsiExit;
		var rsiShortExit = rsiValue < RsiExit;

		var longBreak = isTrending && bullish && candle.ClosePrice > _prevHighest;
		var shortBreak = isTrending && bearish && candle.ClosePrice < _prevLowest;

		if (rsiLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_currentTrade = TradeType.Rsi;
			_lastTradeType = "RSI";
			_lastDirection = "Long";
		}
		else if (rsiShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_currentTrade = TradeType.Rsi;
			_lastTradeType = "RSI";
			_lastDirection = "Short";
		}
		else if (longBreak && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_currentTrade = TradeType.Breakout;
			_lastTradeType = "Breakout";
			_lastDirection = "Long";
			_breakoutStop = candle.ClosePrice - atrValue * AtrMultiplier;
		}
		else if (shortBreak && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_currentTrade = TradeType.Breakout;
			_lastTradeType = "Breakout";
			_lastDirection = "Short";
			_breakoutStop = candle.ClosePrice + atrValue * AtrMultiplier;
		}

		if (_currentTrade == TradeType.Rsi)
		{
			if (Position > 0 && rsiLongExit)
			{
				SellMarket(Position);
				_currentTrade = TradeType.None;
			}
			else if (Position < 0 && rsiShortExit)
			{
				BuyMarket(-Position);
				_currentTrade = TradeType.None;
			}
		}
		else if (_currentTrade == TradeType.Breakout)
		{
			if (Position > 0)
			{
				var trail = candle.ClosePrice - atrValue * AtrMultiplier;
				if (trail > _breakoutStop)
					_breakoutStop = trail;

				if (candle.LowPrice <= _breakoutStop)
				{
					SellMarket(Position);
					_currentTrade = TradeType.None;
				}
			}
			else if (Position < 0)
			{
				var trail = candle.ClosePrice + atrValue * AtrMultiplier;
				if (trail < _breakoutStop)
					_breakoutStop = trail;

				if (candle.HighPrice >= _breakoutStop)
				{
					BuyMarket(-Position);
					_currentTrade = TradeType.None;
				}
			}
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tendency EMA + RSI Strategy - uses EMA crossover with RSI and trend filter
/// </summary>
public class TendencyEmaRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _emaA;
	private ExponentialMovingAverage _emaB;
	private ExponentialMovingAverage _emaC;

	private decimal _previousEmaA;
	private decimal _previousEmaB;
	private bool _emaCrossedOver;
	private bool _emaCrossedUnder;
	private int _barsInPosition;
	private decimal _entryPrice;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// First EMA length (fast).
	/// </summary>
	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	/// <summary>
	/// Second EMA length (medium).
	/// </summary>
	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	/// <summary>
	/// Third EMA length (slow/trend).
	/// </summary>
	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	/// <summary>
	/// Close after X bars if in profit.
	/// </summary>
	public bool CloseAfterXBars
	{
		get => _closeAfterXBars.Value;
		set => _closeAfterXBars.Value = value;
	}

	/// <summary>
	/// Number of bars to close position.
	/// </summary>
	public int XBars
	{
		get => _xBars.Value;
		set => _xBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TendencyEmaRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_emaALength = Param(nameof(EmaALength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Fast EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 3);

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Medium EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(15, 30, 5);

		_emaCLength = Param(nameof(EmaCLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Slow/Trend EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long Entries", "Enable long entries", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short Entries", "Enable short entries", "Strategy");

		_closeAfterXBars = Param(nameof(CloseAfterXBars), true)
			.SetDisplay("Close After X Bars", "Close after X bars if in profit", "Strategy");

		_xBars = Param(nameof(XBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("X Bars", "Number of bars to close position", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);
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

		_barsInPosition = default;
		_entryPrice = default;
		_previousEmaA = default;
		_previousEmaB = default;
		_emaCrossedOver = default;
		_emaCrossedUnder = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_emaA = new ExponentialMovingAverage { Length = EmaALength };
		_emaB = new ExponentialMovingAverage { Length = EmaBLength };
		_emaC = new ExponentialMovingAverage { Length = EmaCLength };

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _emaA, _emaB, _emaC, ProcessCandle)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaA);
			DrawIndicator(area, _emaB);
			DrawIndicator(area, _emaC);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaAValue, decimal emaBValue, decimal emaCValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_rsi.IsFormed || !_emaA.IsFormed || !_emaB.IsFormed || !_emaC.IsFormed)
			return;

		var currentPrice = candle.ClosePrice;
		var openPrice = candle.OpenPrice;

		// Detect EMA crossovers
		if (_previousEmaA != 0 && _previousEmaB != 0)
		{
			_emaCrossedOver = _previousEmaA <= _previousEmaB && emaAValue > emaBValue;
			_emaCrossedUnder = _previousEmaA >= _previousEmaB && emaAValue < emaBValue;
		}

		// Track bars in position
		if (Position != 0)
		{
			_barsInPosition++;
		}
		else
		{
			_barsInPosition = 0;
			_entryPrice = 0;
		}

		CheckEntryConditions(candle, rsiValue, emaAValue, emaBValue, emaCValue);
		CheckExitConditions(candle, rsiValue, emaAValue, emaBValue, emaCValue);

		// Store previous values
		_previousEmaA = emaAValue;
		_previousEmaB = emaBValue;
	}

	private void CheckEntryConditions(ICandleMessage candle, decimal rsiValue, decimal emaAValue, decimal emaBValue, decimal emaCValue)
	{
		var currentPrice = candle.ClosePrice;
		var openPrice = candle.OpenPrice;

		// Long entry: EMA A crosses over EMA B, EMA A > EMA C (trend filter), bullish candle
		if (ShowLong && 
			_emaCrossedOver && 
			emaAValue > emaCValue && 
			currentPrice > openPrice && 
			Position == 0)
		{
			_entryPrice = openPrice;
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
		}

		// Short entry: EMA A crosses under EMA B, EMA A < EMA C (trend filter), bearish candle
		if (ShowShort && 
			_emaCrossedUnder && 
			emaAValue < emaCValue && 
			currentPrice < openPrice && 
			Position == 0)
		{
			_entryPrice = openPrice;
			RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
		}
	}

	private void CheckExitConditions(ICandleMessage candle, decimal rsiValue, decimal emaAValue, decimal emaBValue, decimal emaCValue)
	{
		var currentPrice = candle.ClosePrice;

		// Exit long: RSI > 70
		if (Position > 0 && rsiValue > 70)
		{
			RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			return;
		}

		// Exit short: RSI < 30
		if (Position < 0 && rsiValue < 30)
		{
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));
			return;
		}

		// Close after X bars if in profit
		if (CloseAfterXBars && _barsInPosition >= XBars && _entryPrice != 0)
		{
			var isLongProfitable = Position > 0 && currentPrice > _entryPrice;
			var isShortProfitable = Position < 0 && currentPrice < _entryPrice;

			if (isLongProfitable)
			{
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			}
			else if (isShortProfitable)
			{
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));
			}
		}
	}
}
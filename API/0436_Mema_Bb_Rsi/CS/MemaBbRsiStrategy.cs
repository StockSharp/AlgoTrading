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
/// Multi-timeframe EMA + BB + RSI Strategy
/// </summary>
public class MemaBbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<bool> _useSL;

	private ExponentialMovingAverage _ma1;
	private ExponentialMovingAverage _ma2;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;

	private int _barsInPosition;
	private decimal? _entryPrice;

	public MemaBbRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// Moving Averages
		_ma1Period = Param(nameof(Ma1Period), 10)
			.SetDisplay("MA1 Period", "First EMA period", "Moving Average");

		_ma2Period = Param(nameof(Ma2Period), 55)
			.SetDisplay("MA2 Period", "Second EMA period", "Moving Average");

		// Bollinger Bands
		_bbLength = Param(nameof(BBLength), 20)
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		// RSI
		_rsiLength = Param(nameof(RSILength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOversold = Param(nameof(RSIOversold), 71)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		// Strategy
		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		_closeAfterXBars = Param(nameof(CloseAfterXBars), false)
			.SetDisplay("Close after X bars", "Close position after X bars if in profit", "Strategy");

		_xBars = Param(nameof(XBars), 12)
			.SetDisplay("# bars", "Number of bars", "Strategy");

		_useSL = Param(nameof(UseSL), false)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
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

	public int RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

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
		_ma1 = new ExponentialMovingAverage { Length = Ma1Period };
		_ma2 = new ExponentialMovingAverage { Length = Ma2Period };
		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};
		_rsi = new RelativeStrengthIndex { Length = RSILength };

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ma1, _ma2, _bollinger, _rsi, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1, System.Drawing.Color.Purple);
			DrawIndicator(area, _ma2, System.Drawing.Color.Blue);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue bbValue, IIndicatorValue rsiValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_ma1.IsFormed || !_ma2.IsFormed || !_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		// Get indicator values
		var ma1Price = ma1Value.ToDecimal();
		var ma2Price = ma2Value.ToDecimal();
		var rsiPrice = rsiValue.ToDecimal();

		// Get Bollinger Bands values
		var bollingerTyped = (BollingerBandsValue)bbValue;
		var upper = bollingerTyped.UpBand;
		var lower = bollingerTyped.LowBand;
		var basis = bollingerTyped.MovingAverage;

		// Entry conditions
		var entryLong = candle.ClosePrice > ma1Price && candle.LowPrice < lower;
		var entryShort = candle.ClosePrice < ma1Price && candle.HighPrice > upper && rsiPrice > 50;

		// Exit conditions
		var exitLong = rsiPrice > RSIOversold;
		var exitShort = candle.ClosePrice < lower;

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

		// Close after X bars if in profit
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
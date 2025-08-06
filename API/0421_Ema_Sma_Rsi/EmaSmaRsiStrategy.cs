namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA/SMA + RSI Strategy
/// </summary>
public class EmaSmaRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;

	private ExponentialMovingAverage _emaA;
	private ExponentialMovingAverage _emaB;
	private ExponentialMovingAverage _emaC;
	private RelativeStrengthIndex _rsi;

	private int _barsInPosition;
	private decimal? _entryPrice;

	public EmaSmaRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// Moving Averages
		_emaALength = Param(nameof(EmaALength), 10)
			.SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 100)
			.SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages");

		// RSI
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		// Strategy
		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		_closeAfterXBars = Param(nameof(CloseAfterXBars), true)
			.SetDisplay("Close after X bars", "Close position after X bars if in profit", "Strategy");

		_xBars = Param(nameof(XBars), 24)
			.SetDisplay("# bars", "Number of bars", "Strategy");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
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

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_emaA = new ExponentialMovingAverage { Length = EmaALength };
		_emaB = new ExponentialMovingAverage { Length = EmaBLength };
		_emaC = new ExponentialMovingAverage { Length = EmaCLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaA, _emaB, _emaC, _rsi, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaA, System.Drawing.Color.Purple);
			DrawIndicator(area, _emaB, System.Drawing.Color.Orange);
			DrawIndicator(area, _emaC, System.Drawing.Color.Green);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaA, decimal emaB, decimal emaC, decimal rsi)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_emaA.IsFormed || !_emaB.IsFormed || !_emaC.IsFormed || !_rsi.IsFormed)
			return;

		// Get previous values for crossover detection
		var prevEmaA = _emaA.GetValue(1);
		var prevEmaB = _emaB.GetValue(1);

		// Entry conditions
		var entryLong = emaA > emaB && prevEmaA <= prevEmaB && // Crossover
						emaA > emaC && 
						candle.ClosePrice > candle.OpenPrice;

		var entryShort = emaA < emaB && prevEmaA >= prevEmaB && // Crossunder
						 emaA < emaC && 
						 candle.ClosePrice < candle.OpenPrice;

		// Exit conditions
		var exitLong = rsi > 70;
		var exitShort = rsi < 30;

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
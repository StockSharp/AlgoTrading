using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with RSI exits and trailing stop.
/// Uses three EMAs to define trend and optional time exit.
/// </summary>
public class EmaRsiTrailStopStrategy : Strategy
{
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _exitLongRsi;
	private readonly StrategyParam<int> _exitShortRsi;
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<decimal> _fixStopLossPercent;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private int _barsInPosition;
	private decimal _prevEmaA;
	private decimal _prevEmaB;
	private bool _initialized;

	/// <summary>
	/// Period for EMA A.
	/// </summary>
	public int EmaALength { get => _emaALength.Value; set => _emaALength.Value = value; }

	/// <summary>
	/// Period for EMA B.
	/// </summary>
	public int EmaBLength { get => _emaBLength.Value; set => _emaBLength.Value = value; }

	/// <summary>
	/// Period for EMA C.
	/// </summary>
	public int EmaCLength { get => _emaCLength.Value; set => _emaCLength.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI level to exit long.
	/// </summary>
	public int ExitLongRsi { get => _exitLongRsi.Value; set => _exitLongRsi.Value = value; }

	/// <summary>
	/// RSI level to exit short.
	/// </summary>
	public int ExitShortRsi { get => _exitShortRsi.Value; set => _exitShortRsi.Value = value; }

	/// <summary>
	/// Distance for trailing stop in price units.
	/// </summary>
	public decimal TrailPoints { get => _trailPoints.Value; set => _trailPoints.Value = value; }

	/// <summary>
	/// Profit in price units to activate trailing.
	/// </summary>
	public decimal TrailOffset { get => _trailOffset.Value; set => _trailOffset.Value = value; }

	/// <summary>
	/// Fixed stop loss percent from entry price.
	/// </summary>
	public decimal FixStopLossPercent { get => _fixStopLossPercent.Value; set => _fixStopLossPercent.Value = value; }

	/// <summary>
	/// Close trade after X bars if profitable.
	/// </summary>
	public bool CloseAfterXBars { get => _closeAfterXBars.Value; set => _closeAfterXBars.Value = value; }

	/// <summary>
	/// Number of bars before closing.
	/// </summary>
	public int XBars { get => _xBars.Value; set => _xBars.Value = value; }

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool ShowLong { get => _showLong.Value; set => _showLong.Value = value; }

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool ShowShort { get => _showShort.Value; set => _showShort.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaRsiTrailStopStrategy()
	{
		_emaALength = Param(nameof(EmaALength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Period for EMA A", "Indicators");

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Period for EMA B", "Indicators");

		_emaCLength = Param(nameof(EmaCLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Period for EMA C", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_exitLongRsi = Param(nameof(ExitLongRsi), 70)
			.SetDisplay("Exit Long RSI", "RSI level to exit long", "Exit");

		_exitShortRsi = Param(nameof(ExitShortRsi), 30)
			.SetDisplay("Exit Short RSI", "RSI level to exit short", "Exit");

		_trailPoints = Param(nameof(TrailPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Points", "Trailing distance", "Trailing Stop");

		_trailOffset = Param(nameof(TrailOffset), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Offset", "Profit to activate trailing", "Trailing Stop");

		_fixStopLossPercent = Param(nameof(FixStopLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Stop Loss %", "Stop loss percent", "Risk");

		_closeAfterXBars = Param(nameof(CloseAfterXBars), true)
			.SetDisplay("Close After X Bars", "Close after X bars if profitable", "Exit");

		_xBars = Param(nameof(XBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("X Bars", "Bars before close", "Exit");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "Mode");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Enable Short", "Allow short entries", "Mode");

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

		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_barsInPosition = 0;
		_prevEmaA = 0m;
		_prevEmaB = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaA = new ExponentialMovingAverage { Length = EmaALength };
		var emaB = new ExponentialMovingAverage { Length = EmaBLength };
		var emaC = new ExponentialMovingAverage { Length = EmaCLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaA, emaB, emaC, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaA);
			DrawIndicator(area, emaB);
			DrawIndicator(area, emaC);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaAVal, decimal emaBVal, decimal emaCVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevEmaA = emaAVal;
			_prevEmaB = emaBVal;
			_initialized = true;
			return;
		}

		var crossAbove = _prevEmaA <= _prevEmaB && emaAVal > emaBVal;
		var crossBelow = _prevEmaA >= _prevEmaB && emaAVal < emaBVal;

		if (Position == 0)
		{
			if (crossAbove && emaAVal > emaCVal && candle.ClosePrice > candle.OpenPrice && ShowLong)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.HighPrice;
				_trailingReset();
			}
			else if (crossBelow && emaAVal < emaCVal && candle.ClosePrice < candle.OpenPrice && ShowShort)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_lowestPrice = candle.LowPrice;
				_trailingReset();
			}
		}
		else
		{
			_barsInPosition++;

			if (Position > 0)
			{
				_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

				var stop = _entryPrice * (1m - FixStopLossPercent / 100m);

				if (_highestPrice - _entryPrice >= TrailOffset)
					stop = Math.Max(stop, _highestPrice - TrailPoints);

				if ((CloseAfterXBars && _barsInPosition >= XBars && candle.ClosePrice > _entryPrice) ||
					rsiVal > ExitLongRsi ||
					candle.ClosePrice <= stop)
				{
					SellMarket();
					_trailingReset();
				}
			}
			else if (Position < 0)
			{
				_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

				var stop = _entryPrice * (1m + FixStopLossPercent / 100m);

				if (_entryPrice - _lowestPrice >= TrailOffset)
					stop = Math.Min(stop, _lowestPrice + TrailPoints);

				if ((CloseAfterXBars && _barsInPosition >= XBars && candle.ClosePrice < _entryPrice) ||
					rsiVal < ExitShortRsi ||
					candle.ClosePrice >= stop)
				{
					BuyMarket();
					_trailingReset();
				}
			}
		}

		_prevEmaA = emaAVal;
		_prevEmaB = emaBVal;
	}

	private void _trailingReset()
	{
		_barsInPosition = 0;
		_prevEmaA = 0m;
		_prevEmaB = 0m;
		_initialized = false;
	}
}

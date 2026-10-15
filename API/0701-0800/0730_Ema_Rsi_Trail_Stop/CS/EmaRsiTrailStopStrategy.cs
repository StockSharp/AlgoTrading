using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA RSI trail stop strategy.
/// EMA A crossing above EMA B with the close above EMA C on a bullish candle goes long, the mirrored conditions go short.
/// RSI above ExitLongRsi closes a long and RSI below ExitShortRsi closes a short. A fixed percent stop protects the position
/// and turns into a trailing stop TrailPoints price steps behind the best price once price has moved TrailOffset steps in favour.
/// Optionally a profitable position is closed after XBars candles.
/// </summary>
public class EmaRsiTrailStopStrategy : Strategy
{
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _exitLongRsi;
	private readonly StrategyParam<decimal> _exitShortRsi;
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<decimal> _fixStopLossPercent;
	private readonly StrategyParam<bool> _closeAfterXBars;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevA;
	private decimal? _prevB;
	private decimal _entryPrice;
	private decimal _bestPrice;
	private decimal? _stopPrice;
	private int _barsInPosition;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	/// <summary>
	/// Medium EMA length.
	/// </summary>
	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level that closes a long.
	/// </summary>
	public decimal ExitLongRsi
	{
		get => _exitLongRsi.Value;
		set => _exitLongRsi.Value = value;
	}

	/// <summary>
	/// RSI level that closes a short.
	/// </summary>
	public decimal ExitShortRsi
	{
		get => _exitShortRsi.Value;
		set => _exitShortRsi.Value = value;
	}

	/// <summary>
	/// Trailing distance in price steps.
	/// </summary>
	public decimal TrailPoints
	{
		get => _trailPoints.Value;
		set => _trailPoints.Value = value;
	}

	/// <summary>
	/// Favourable move in price steps that activates the trailing stop.
	/// </summary>
	public decimal TrailOffset
	{
		get => _trailOffset.Value;
		set => _trailOffset.Value = value;
	}

	/// <summary>
	/// Fixed stop loss percentage from entry price.
	/// </summary>
	public decimal FixStopLossPercent
	{
		get => _fixStopLossPercent.Value;
		set => _fixStopLossPercent.Value = value;
	}

	/// <summary>
	/// Close a profitable position after XBars candles.
	/// </summary>
	public bool CloseAfterXBars
	{
		get => _closeAfterXBars.Value;
		set => _closeAfterXBars.Value = value;
	}

	/// <summary>
	/// Candles after which a profitable position is closed.
	/// </summary>
	public int XBars
	{
		get => _xBars.Value;
		set => _xBars.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaRsiTrailStopStrategy()
	{
		_emaALength = Param(nameof(EmaALength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA A", "Fast EMA length", "Indicators");

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA B", "Medium EMA length", "Indicators");

		_emaCLength = Param(nameof(EmaCLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA C", "Trend EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_exitLongRsi = Param(nameof(ExitLongRsi), 70m)
			.SetDisplay("Exit Long RSI", "RSI level that closes a long", "Exits");

		_exitShortRsi = Param(nameof(ExitShortRsi), 30m)
			.SetDisplay("Exit Short RSI", "RSI level that closes a short", "Exits");

		_trailPoints = Param(nameof(TrailPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Trail Points", "Trailing distance in price steps", "Risk");

		_trailOffset = Param(nameof(TrailOffset), 10m)
			.SetNotNegative()
			.SetDisplay("Trail Offset", "Favourable move in price steps that activates trailing", "Risk");

		_fixStopLossPercent = Param(nameof(FixStopLossPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Fixed Stop %", "Fixed stop loss percentage from entry price", "Risk");

		_closeAfterXBars = Param(nameof(CloseAfterXBars), true)
			.SetDisplay("Close After X Bars", "Close a profitable position after XBars candles", "Exits");

		_xBars = Param(nameof(XBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("X Bars", "Candles after which a profitable position is closed", "Exits");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long Trades", "Allow long trades", "General");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short Trades", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevA = null;
		_prevB = null;
		_entryPrice = 0m;
		_bestPrice = 0m;
		_stopPrice = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal a, decimal b, decimal c, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevA = _prevA;
		var prevB = _prevB;
		_prevA = a;
		_prevB = b;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 && ManagePosition(candle, rsi))
			return;

		if (prevA is not decimal pa || prevB is not decimal pb)
			return;

		var crossUp = pa <= pb && a > b;
		var crossDown = pa >= pb && a < b;

		if (ShowLong && crossUp && candle.ClosePrice > c && candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			OpenPosition(candle.ClosePrice, true);
		}
		else if (ShowShort && crossDown && candle.ClosePrice < c && candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			OpenPosition(candle.ClosePrice, false);
		}
	}

	private void OpenPosition(decimal price, bool isLong)
	{
		_entryPrice = price;
		_bestPrice = price;
		_barsInPosition = 0;
		_stopPrice = FixStopLossPercent > 0m
			? (isLong ? price * (1m - FixStopLossPercent / 100m) : price * (1m + FixStopLossPercent / 100m))
			: null;
	}

	private bool ManagePosition(ICandleMessage candle, decimal rsi)
	{
		var step = Security?.PriceStep ?? 1m;
		_barsInPosition++;

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return true;
			}

			if (rsi > ExitLongRsi || (CloseAfterXBars && _barsInPosition >= XBars && candle.ClosePrice > _entryPrice))
			{
				SellMarket(Position);
				return true;
			}

			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);

			if (_bestPrice - _entryPrice >= TrailOffset * step)
			{
				var trail = _bestPrice - TrailPoints * step;
				_stopPrice = _stopPrice is decimal current ? Math.Max(current, trail) : trail;
			}
		}
		else
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				return true;
			}

			if (rsi < ExitShortRsi || (CloseAfterXBars && _barsInPosition >= XBars && candle.ClosePrice < _entryPrice))
			{
				BuyMarket(-Position);
				return true;
			}

			_bestPrice = Math.Min(_bestPrice, candle.LowPrice);

			if (_entryPrice - _bestPrice >= TrailOffset * step)
			{
				var trail = _bestPrice + TrailPoints * step;
				_stopPrice = _stopPrice is decimal current ? Math.Min(current, trail) : trail;
			}
		}

		return false;
	}
}

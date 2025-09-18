using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish/Bearish Engulfing strategy with RSI confirmation converted from the MetaTrader Expert_ABE_BE_RSI advisor.
/// </summary>
public class AbeBeRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _bullishEntryLevel;
	private readonly StrategyParam<decimal> _bearishEntryLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _closeAverage = null!;
	private SimpleMovingAverage _bodyAverage = null!;

	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal? _prevPrevOpen;
	private decimal? _prevPrevClose;

	private decimal? _prevRsi;
	private decimal? _prevPrevRsi;

	private decimal? _prevBodyAverage;
	private decimal? _prevCloseAverage;
	private decimal? _prevPrevCloseAverage;

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Period of the RSI confirmation indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Period used for the candle body average filter.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// RSI level that validates bullish engulfing entries.
	/// </summary>
	public decimal BullishEntryLevel
	{
		get => _bullishEntryLevel.Value;
		set => _bullishEntryLevel.Value = value;
	}

	/// <summary>
	/// RSI level that validates bearish engulfing entries.
	/// </summary>
	public decimal BearishEntryLevel
	{
		get => _bearishEntryLevel.Value;
		set => _bearishEntryLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold used for exit cross detection.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold used for exit cross detection.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AbeBeRsiStrategy"/>.
	/// </summary>
	public AbeBeRsiStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Default order volume", "Trading");

		_rsiPeriod = Param(nameof(RsiPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI filter", "Indicators");

		_maPeriod = Param(nameof(MovingAveragePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length for the candle body average", "Indicators");

		_bullishEntryLevel = Param(nameof(BullishEntryLevel), 40m)
			.SetDisplay("Bullish Entry RSI", "RSI must be below this level for bullish signals", "Signals");

		_bearishEntryLevel = Param(nameof(BearishEntryLevel), 60m)
			.SetDisplay("Bearish Entry RSI", "RSI must be above this level for bearish signals", "Signals");

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 30m)
			.SetDisplay("Exit Lower RSI", "Lower RSI crossing level for exits", "Risk");

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 70m)
			.SetDisplay("Exit Upper RSI", "Upper RSI crossing level for exits", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used by the strategy", "General");
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

		_rsi = null!;
		_closeAverage = null!;
		_bodyAverage = null!;

		_prevOpen = null;
		_prevClose = null;
		_prevPrevOpen = null;
		_prevPrevClose = null;

		_prevRsi = null;
		_prevPrevRsi = null;

		_prevBodyAverage = null;
		_prevCloseAverage = null;
		_prevPrevCloseAverage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_closeAverage = new SimpleMovingAverage { Length = MovingAveragePeriod };
		_bodyAverage = new SimpleMovingAverage { Length = MovingAveragePeriod };

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Feed the moving averages with the newly completed candle.
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyAverageValue = _bodyAverage.Process(body, candle.OpenTime, true).ToNullableDecimal();
		var closeAverageValue = _closeAverage.Process(candle.ClosePrice, candle.OpenTime, true).ToNullableDecimal();

		// Cache previously stored candle and indicator values for clarity.
		var prevOpen = _prevOpen;
		var prevClose = _prevClose;
		var prevPrevOpen = _prevPrevOpen;
		var prevPrevClose = _prevPrevClose;
		var prevRsi = _prevRsi;
		var prevPrevRsi = _prevPrevRsi;
		var prevBodyAverage = _prevBodyAverage;
		var prevPrevCloseAverage = _prevPrevCloseAverage;

		if (IsFormedAndOnlineAndAllowTrading() &&
			prevOpen is decimal lastOpen &&
			prevClose is decimal lastClose &&
			prevPrevOpen is decimal olderOpen &&
			prevPrevClose is decimal olderClose &&
			prevBodyAverage is decimal bodyFilter &&
			prevPrevCloseAverage is decimal trendFilter &&
			prevRsi is decimal lastRsi &&
			prevPrevRsi is decimal olderRsi)
		{
			// Evaluate engulfing patterns on the two previously closed candles.
			var bullishEngulfing = olderOpen > olderClose &&
				lastClose > lastOpen &&
				lastClose - lastOpen > bodyFilter &&
				lastClose > olderOpen &&
				(lastOpen < olderClose) &&
				(olderOpen + olderClose) / 2m < trendFilter;

			var bearishEngulfing = olderOpen < olderClose &&
				lastOpen > lastClose &&
				lastOpen - lastClose > bodyFilter &&
				lastClose < olderOpen &&
				(lastOpen > olderClose) &&
				(olderOpen + olderClose) / 2m > trendFilter;

			// Combine the candlestick signal with RSI filters for entries.
			var longEntry = bullishEngulfing && lastRsi < BullishEntryLevel;
			var shortEntry = bearishEngulfing && lastRsi > BearishEntryLevel;

			// Detect RSI crossings to manage opposite positions.
			var closeShort = (lastRsi > ExitLowerLevel && olderRsi < ExitLowerLevel) ||
				(lastRsi > ExitUpperLevel && olderRsi < ExitUpperLevel);

			var closeLong = (lastRsi < ExitUpperLevel && olderRsi > ExitUpperLevel) ||
				(lastRsi < ExitLowerLevel && olderRsi > ExitLowerLevel);

			if (Position < 0m && closeShort)
			{
				BuyMarket(Math.Abs(Position));
			}
			else if (Position > 0m && closeLong)
			{
				SellMarket(Math.Abs(Position));
			}

			if (longEntry && Position <= 0m)
			{
				var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
				BuyMarket(volume);
			}
			else if (shortEntry && Position >= 0m)
			{
				var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);
				SellMarket(volume);
			}
		}

		// Persist the latest bar information for the next iteration.
		UpdateState(candle, rsiValue, bodyAverageValue, closeAverageValue);
	}

	private void UpdateState(ICandleMessage candle, decimal rsiValue, decimal? bodyAverageValue, decimal? closeAverageValue)
	{
		_prevPrevOpen = _prevOpen;
		_prevPrevClose = _prevClose;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;

		_prevPrevRsi = _prevRsi;
		_prevRsi = _rsi.IsFormed ? rsiValue : null;

		_prevBodyAverage = bodyAverageValue;

		_prevPrevCloseAverage = _prevCloseAverage;
		_prevCloseAverage = closeAverageValue;
	}
}

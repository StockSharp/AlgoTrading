using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold scalping strategy with precise entries.
/// The trend is up when the EmaFastPeriod EMA is above the EmaSlowPeriod EMA. With RSI between RsiLower and RsiUpper, a bullish
/// engulfing candle that touches the fast EMA in an uptrend goes long and a bearish engulfing candle touching it in a downtrend goes
/// short. The stop is one ATR from the entry and the target PipTarget price steps away.
/// </summary>
public class GoldScalpingWithPreciseEntriesStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _pipTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
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
	/// ATR period for the stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lower bound of the RSI range.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// Upper bound of the RSI range.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal PipTarget
	{
		get => _pipTarget.Value;
		set => _pipTarget.Value = value;
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
	public GoldScalpingWithPreciseEntriesStrategy()
	{
		_emaFastPeriod = Param(nameof(EmaFastPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast Period", "Fast EMA period", "Indicators");

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow Period", "Slow EMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for the stop", "Risk");

		_rsiLower = Param(nameof(RsiLower), 45m)
			.SetDisplay("RSI Lower", "Lower bound of the RSI range", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 55m)
			.SetDisplay("RSI Upper", "Upper bound of the RSI range", "Indicators");

		_pipTarget = Param(nameof(PipTarget), 2m)
			.SetNotNegative()
			.SetDisplay("Pip Target", "Take profit distance in price steps", "Risk");

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
		_prevOpen = null;
		_prevClose = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var emaFast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevOpen = _prevOpen;
		var prevClose = _prevClose;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;

		if (ManagePosition(candle))
			return;

		if (prevOpen is not decimal po || prevClose is not decimal pc)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || Position != 0)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var rsiInRange = rsi >= RsiLower && rsi <= RsiUpper;
		var touchesEma = candle.LowPrice <= emaFast && candle.HighPrice >= emaFast;
		var bullishEngulfing = pc < po && close > open && open <= pc && close >= po;
		var bearishEngulfing = pc > po && close < open && open >= pc && close <= po;
		var target = PipTarget * (Security?.PriceStep ?? 1m);

		if (emaFast > emaSlow && rsiInRange && touchesEma && bullishEngulfing)
		{
			BuyMarket(Volume);
			_stopPrice = close - atr;
			_takePrice = close + target;
		}
		else if (emaFast < emaSlow && rsiInRange && touchesEma && bearishEngulfing)
		{
			SellMarket(Volume);
			_stopPrice = close + atr;
			_takePrice = close - target;
		}
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CBC with Trend Confirmation and Separate Stop Loss strategy.
/// The color bar change (CBC) state turns bullish when the close breaks above the previous candle's high and bearish when it breaks
/// below the previous candle's low. A flip to bullish goes long when the slow EMA is above the daily VWAP, a flip to bearish goes short
/// when it is below, reversing an opposite position; with StrongFlipsOnly the flip candle also has to close in its direction. Entries
/// are taken only between EntryStartHour and EntryEndHour (UTC). Each entry freezes a target ProfitTargetMultiplier ATRs away and a
/// stop at the previous candle's low (long) or high (short).
/// </summary>
public class CbcWithTrendConfirmationAndSeparateStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _profitTargetMultiplier;
	private readonly StrategyParam<bool> _strongFlipsOnly;
	private readonly StrategyParam<int> _entryStartHour;
	private readonly StrategyParam<int> _entryEndHour;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private bool? _cbcBullish;
	private DateTime _vwapDay;
	private decimal _vwapPriceVolume;
	private decimal _vwapVolume;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple of the profit target.
	/// </summary>
	public decimal ProfitTargetMultiplier
	{
		get => _profitTargetMultiplier.Value;
		set => _profitTargetMultiplier.Value = value;
	}

	/// <summary>
	/// Trade only flips whose candle closes in the flip direction.
	/// </summary>
	public bool StrongFlipsOnly
	{
		get => _strongFlipsOnly.Value;
		set => _strongFlipsOnly.Value = value;
	}

	/// <summary>
	/// First entry hour (UTC).
	/// </summary>
	public int EntryStartHour
	{
		get => _entryStartHour.Value;
		set => _entryStartHour.Value = value;
	}

	/// <summary>
	/// Hour entries stop (UTC, exclusive).
	/// </summary>
	public int EntryEndHour
	{
		get => _entryEndHour.Value;
		set => _entryEndHour.Value = value;
	}

	/// <summary>
	/// Period of the slow trend EMA.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
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
	public CbcWithTrendConfirmationAndSeparateStopLossStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_profitTargetMultiplier = Param(nameof(ProfitTargetMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target Multiplier", "ATR multiple of the profit target", "Risk");

		_strongFlipsOnly = Param(nameof(StrongFlipsOnly), true)
			.SetDisplay("Strong Flips Only", "Trade only flips whose candle closes in the flip direction", "Signals");

		_entryStartHour = Param(nameof(EntryStartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Entry Start Hour", "First entry hour (UTC)", "Session");

		_entryEndHour = Param(nameof(EntryEndHour), 15)
			.SetRange(1, 24)
			.SetDisplay("Entry End Hour", "Hour entries stop (UTC, exclusive)", "Session");

		_slowEmaLength = Param(nameof(SlowEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period of the slow trend EMA", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevCandle = null;
		_cbcBullish = null;
		_vwapDay = default;
		_vwapPriceVolume = 0m;
		_vwapVolume = 0m;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Session VWAP restarts every UTC day.
		var day = candle.OpenTime.Date;

		if (day != _vwapDay)
		{
			_vwapDay = day;
			_vwapPriceVolume = 0m;
			_vwapVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_vwapPriceVolume += typical * candle.TotalVolume;
		_vwapVolume += candle.TotalVolume;

		var prev = _prevCandle;
		_prevCandle = candle;

		if (prev == null)
			return;

		var previousState = _cbcBullish;

		if (candle.ClosePrice > prev.HighPrice)
			_cbcBullish = true;
		else if (candle.ClosePrice < prev.LowPrice)
			_cbcBullish = false;

		if (!emaValue.IsFormed || !atrValue.IsFormed || _vwapVolume <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Stop and target frozen at entry.
		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget && (candle.LowPrice <= longStop || candle.HighPrice >= longTarget))
		{
			SellMarket(Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget && (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (previousState is not bool wasBullish || _cbcBullish is not bool isBullish || wasBullish == isBullish)
			return;

		var hour = candle.OpenTime.Hour;

		if (hour < EntryStartHour || hour >= EntryEndHour)
			return;

		var ema = emaValue.GetValue<decimal>();
		var vwap = _vwapPriceVolume / _vwapVolume;
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (isBullish && (!StrongFlipsOnly || close > candle.OpenPrice) && ema > vwap && Position <= 0 && prev.LowPrice < close)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = prev.LowPrice;
			_targetPrice = close + atr * ProfitTargetMultiplier;
		}
		else if (!isBullish && (!StrongFlipsOnly || close < candle.OpenPrice) && ema < vwap && Position >= 0 && prev.HighPrice > close)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = prev.HighPrice;
			_targetPrice = close - atr * ProfitTargetMultiplier;
		}
	}
}

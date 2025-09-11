using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TradeType
{
	Long,
	Short,
	All
}

/// <summary>
/// Internal Bar Strength mean reversion strategy with optional EMA filter and dollar-cost averaging.
/// </summary>
public class IbsInternalBarStrengthStrategy : Strategy
{
	private readonly StrategyParam<decimal> _ibsEntryThreshold;
	private readonly StrategyParam<decimal> _ibsExitThreshold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _minEntryPct;
	private readonly StrategyParam<int> _maxTradeDuration;
	private readonly StrategyParam<TradeType> _entryType;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal? _lastEntryPrice;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// IBS entry threshold.
	/// </summary>
	public decimal IbsEntryThreshold
	{
		get => _ibsEntryThreshold.Value;
		set => _ibsEntryThreshold.Value = value;
	}

	/// <summary>
	/// IBS exit threshold.
	/// </summary>
	public decimal IbsExitThreshold
	{
		get => _ibsExitThreshold.Value;
		set => _ibsExitThreshold.Value = value;
	}

	/// <summary>
	/// EMA period (0 to disable).
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum price drop for new entry in percent.
	/// </summary>
	public decimal MinEntryPct
	{
		get => _minEntryPct.Value;
		set => _minEntryPct.Value = value;
	}

	/// <summary>
	/// Maximum trade duration in days.
	/// </summary>
	public int MaxTradeDuration
	{
		get => _maxTradeDuration.Value;
		set => _maxTradeDuration.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public TradeType EntryType
	{
		get => _entryType.Value;
		set => _entryType.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IbsInternalBarStrengthStrategy"/>.
	/// </summary>
	public IbsInternalBarStrengthStrategy()
	{
		_ibsEntryThreshold = Param(nameof(IbsEntryThreshold), 0.09m)
			.SetDisplay("IBS Entry", "IBS entry threshold", "Signals")
			.SetGreaterThanZero();

		_ibsExitThreshold = Param(nameof(IbsExitThreshold), 0.985m)
			.SetDisplay("IBS Exit", "IBS exit threshold", "Signals")
			.SetGreaterThanZero();

		_emaPeriod = Param(nameof(EmaPeriod), 220)
			.SetDisplay("EMA Period", "EMA period (0 to disable)", "Indicators");

		_minEntryPct = Param(nameof(MinEntryPct), 0m)
			.SetDisplay("Min Entry %", "Minimum distance for new entry (%)", "Risk");

		_maxTradeDuration = Param(nameof(MaxTradeDuration), 14)
			.SetDisplay("Max Duration", "Maximum trade duration in days", "Risk")
			.SetGreaterThanZero();

		_entryType = Param(nameof(EntryType), TradeType.Long)
			.SetDisplay("Entry Type", "Allowed trade directions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevCandle = null;
		_lastEntryPrice = null;
		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		if (EmaPeriod > 0)
		{
			var ema = new ExponentialMovingAverage { Length = EmaPeriod };

			subscription
				.Bind(ema, ProcessCandle)
				.Start();

			var area = CreateChartArea();

			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ema);
				DrawOwnTrades(area);
			}
		}
		else
		{
			subscription
				.Bind(ProcessCandle)
				.Start();

			var area = CreateChartArea();

			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		Process(candle, ema);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		Process(candle, 0m);
	}

	private void Process(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		var range = _prevCandle.HighPrice - _prevCandle.LowPrice;
		var ibs = range != 0m ? (_prevCandle.ClosePrice - _prevCandle.LowPrice) / range : 0.5m;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevCandle = candle;
			return;
		}

		if (Position == 0m)
		{
			_lastEntryPrice = null;
			_entryTime = null;
		}

		var emaLong = EmaPeriod == 0 || candle.ClosePrice > ema;
		var emaShort = EmaPeriod == 0 || candle.ClosePrice < ema;
		var allowLong = EntryType == TradeType.Long || EntryType == TradeType.All;
		var allowShort = EntryType == TradeType.Short || EntryType == TradeType.All;

		var enterLong = ibs < IbsEntryThreshold && emaLong && allowLong;
		var enterShort = ibs > IbsExitThreshold && emaShort && allowShort;

		var dcaLong = _lastEntryPrice == null || (candle.ClosePrice < _lastEntryPrice && (_lastEntryPrice.Value - candle.ClosePrice) / _lastEntryPrice.Value >= MinEntryPct / 100m);
		var dcaShort = _lastEntryPrice == null || (candle.ClosePrice > _lastEntryPrice && (candle.ClosePrice - _lastEntryPrice.Value) / _lastEntryPrice.Value >= MinEntryPct / 100m);

		var duration = _entryTime.HasValue ? (int)(candle.CloseTime.Date - _entryTime.Value.Date).TotalDays : 0;

		if (enterLong && dcaLong)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else
				BuyMarket(Volume);

			_lastEntryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
		}
		else if (enterShort && dcaShort)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else
				SellMarket(Volume);

			_lastEntryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
		}

		var exitLong = ibs > IbsExitThreshold || duration >= MaxTradeDuration;
		var exitShort = ibs < IbsEntryThreshold || duration >= MaxTradeDuration;

		if (Position > 0 && exitLong)
		{
			SellMarket(Math.Abs(Position));
			_lastEntryPrice = null;
			_entryTime = null;
		}
		else if (Position < 0 && exitShort)
		{
			BuyMarket(Math.Abs(Position));
			_lastEntryPrice = null;
			_entryTime = null;
		}

		_prevCandle = candle;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi ROC Percentile strategy.
/// The Heikin Ashi close is smoothed with an SMA of RocLength and its Rate of Change over RocLength candles is measured. The upper
/// band is the highest and the lower band the lowest ROC of the previous RocLength candles. ROC crossing back above the lower band
/// buys and crossing back below the upper band sells, reversing an opposite position. Trading starts at StartDate and a percent
/// stop limits the loss.
/// </summary>
public class HeikinAshiRocPercentileStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;

	private readonly Queue<decimal> _haCloses = new();
	private readonly Queue<decimal> _smoothed = new();
	private decimal _haCloseSum;
	private readonly Queue<decimal> _rocHistory = new();
	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal? _prevRoc;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Length of the smoothing SMA, the ROC and the band window.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Stop loss percent from the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Date trading starts from.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HeikinAshiRocPercentileStrategy()
	{
		_rocLength = Param(nameof(RocLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "Length of the smoothing SMA, the ROC and the band window", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percent from the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Date trading starts from", "General");
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
		_haCloses.Clear();
		_haCloseSum = 0m;
		_smoothed.Clear();
		_rocHistory.Clear();
		_haOpen = null;
		_haClose = null;
		_prevRoc = null;
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal prevHaOpen && _haClose is decimal prevHaClose
			? (prevHaOpen + prevHaClose) / 2m
			: (candle.OpenPrice + candle.ClosePrice) / 2m;

		_haOpen = haOpen;
		_haClose = haClose;

		var length = RocLength;

		// SMA of the Heikin Ashi close.
		_haCloses.Enqueue(haClose);
		_haCloseSum += haClose;
		if (_haCloses.Count > length)
			_haCloseSum -= _haCloses.Dequeue();
		if (_haCloses.Count < length)
			return;

		var smoothed = _haCloseSum / length;

		_smoothed.Enqueue(smoothed);
		if (_smoothed.Count <= length)
			return;

		var past = _smoothed.Dequeue();
		if (past == 0)
			return;

		var roc = (smoothed - past) / past * 100m;

		decimal? upper = null;
		decimal? lower = null;

		if (_rocHistory.Count >= length)
		{
			upper = _rocHistory.Max();
			lower = _rocHistory.Min();
		}

		var prevRoc = _prevRoc;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;

		_rocHistory.Enqueue(roc);
		while (_rocHistory.Count > length)
			_rocHistory.Dequeue();

		_prevRoc = roc;
		_prevUpper = upper;
		_prevLower = lower;

		if (prevRoc is not decimal lastRoc || upper is not decimal up || lower is not decimal low || prevUpper is not decimal lastUp || prevLower is not decimal lastLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartDate.UtcDateTime)
			return;

		if (lastRoc <= lastLow && roc > low && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (lastRoc >= lastUp && roc < up && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

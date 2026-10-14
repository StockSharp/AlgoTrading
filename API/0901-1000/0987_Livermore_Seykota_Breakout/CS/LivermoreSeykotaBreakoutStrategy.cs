using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Livermore Seykota breakout strategy.
/// Tracks the most recent confirmed pivot high and pivot low (PivotLength bars on each side). A close above the last pivot high goes long
/// when price is above the main EMA, the fast EMA is above the slow EMA and volume is above its average; a close below the last pivot low
/// goes short under the mirrored conditions. Positions exit on an ATR stop placed at entry or on an ATR trailing stop.
/// </summary>
public class LivermoreSeykotaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _mainEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private decimal? _pivotHigh;
	private decimal? _pivotLow;
	private decimal? _stopPrice;
	private decimal? _trailPrice;

	/// <summary>
	/// Main EMA trend filter length.
	/// </summary>
	public int MainEmaLength
	{
		get => _mainEmaLength.Value;
		set => _mainEmaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Bars on each side that confirm a pivot.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the initial stop.
	/// </summary>
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMultiplier.Value;
		set => _stopAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the trailing stop.
	/// </summary>
	public decimal TrailAtrMultiplier
	{
		get => _trailAtrMultiplier.Value;
		set => _trailAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeSmaLength
	{
		get => _volumeSmaLength.Value;
		set => _volumeSmaLength.Value = value;
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
	public LivermoreSeykotaBreakoutStrategy()
	{
		_mainEmaLength = Param(nameof(MainEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Main EMA", "Main EMA trend filter length", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_pivotLength = Param(nameof(PivotLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars on each side that confirm a pivot", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 3m)
			.SetNotNegative()
			.SetDisplay("Stop ATR Mult", "ATR multiplier of the initial stop", "Risk");

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Trail ATR Mult", "ATR multiplier of the trailing stop", "Risk");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA", "Volume SMA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_highs.Clear();
		_lows.Clear();
		_pivotHigh = null;
		_pivotLow = null;
		_stopPrice = null;
		_trailPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var mainEma = new ExponentialMovingAverage { Length = MainEmaLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mainEma, fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mainEma);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mainEma, decimal fastEma, decimal slowEma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		UpdatePivots(candle);

		if (Position > 0 && _stopPrice is decimal longStop)
		{
			if (TrailAtrMultiplier > 0)
				_trailPrice = Math.Max(_trailPrice ?? decimal.MinValue, candle.ClosePrice - atr * TrailAtrMultiplier);

			var exitLevel = Math.Max(longStop, _trailPrice ?? decimal.MinValue);
			if (candle.LowPrice <= exitLevel)
			{
				SellMarket(Position);
				_stopPrice = null;
				_trailPrice = null;
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop)
		{
			if (TrailAtrMultiplier > 0)
				_trailPrice = Math.Min(_trailPrice ?? decimal.MaxValue, candle.ClosePrice + atr * TrailAtrMultiplier);

			var exitLevel = Math.Min(shortStop, _trailPrice ?? decimal.MaxValue);
			if (candle.HighPrice >= exitLevel)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_trailPrice = null;
				return;
			}
		}

		if (!volumeValue.IsFormed || _pivotHigh is not decimal pivotHigh || _pivotLow is not decimal pivotLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var volumeStrong = candle.TotalVolume > volumeValue.GetValue<decimal>();
		var upTrend = close > mainEma && fastEma > slowEma;
		var downTrend = close < mainEma && fastEma < slowEma;

		if (close > pivotHigh && upTrend && volumeStrong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = StopAtrMultiplier > 0 ? close - atr * StopAtrMultiplier : decimal.MinValue;
			_trailPrice = null;
		}
		else if (close < pivotLow && downTrend && volumeStrong && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = StopAtrMultiplier > 0 ? close + atr * StopAtrMultiplier : decimal.MaxValue;
			_trailPrice = null;
		}
	}

	private void UpdatePivots(ICandleMessage candle)
	{
		var window = PivotLength * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > window)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < window)
			return;

		// The middle bar is a pivot once PivotLength bars on each side have closed.
		var centerHigh = _highs[PivotLength];
		var centerLow = _lows[PivotLength];

		if (centerHigh == _highs.Max())
			_pivotHigh = centerHigh;

		if (centerLow == _lows.Min())
			_pivotLow = centerLow;
	}
}

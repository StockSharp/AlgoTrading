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
/// Breakouts with time filter strategy.
/// A close above the highest high of the previous Length candles goes long and a close below their lowest low goes short, only inside
/// the 14:30-15:00 UTC window when UseTimeFilter is set and only on the matching side of the moving average when UseMaFilter is set.
/// The stop is placed by ATR, by the candle extremes or by fixed points, and the target sits RiskReward times the stop distance away.
/// </summary>
public class BreakoutsWithTimeFilterStrategy : Strategy
{
	/// <summary>
	/// Stop loss placement methods.
	/// </summary>
	public enum StopLossTypes
	{
		/// <summary>
		/// ATR multiple from the entry close.
		/// </summary>
		Atr,

		/// <summary>
		/// Extreme of the recent candles.
		/// </summary>
		Candle,

		/// <summary>
		/// Fixed number of price steps.
		/// </summary>
		Points,
	}

	private static readonly TimeSpan _sessionStart = new(14, 30, 0);
	private static readonly TimeSpan _sessionEnd = new(15, 0, 0);

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<StopLossTypes> _slType;
	private readonly StrategyParam<int> _slLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _pointsStop;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal high, decimal low)> _bars = new();
	private decimal _stopLevel;
	private decimal _targetLevel;

	/// <summary>
	/// Previous candles the breakout levels span.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Period of the moving average filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Require the close on the trade side of the moving average.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// Allow entries only between 14:30 and 15:00 UTC.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Stop loss placement method.
	/// </summary>
	public StopLossTypes SlType
	{
		get => _slType.Value;
		set => _slType.Value = value;
	}

	/// <summary>
	/// Earlier candles whose extreme sets a candle-based stop; 0 uses the entry candle only.
	/// </summary>
	public int SlLength
	{
		get => _slLength.Value;
		set => _slLength.Value = value;
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
	/// ATR multiple of the ATR stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop distance in price steps for the points stop.
	/// </summary>
	public decimal PointsStop
	{
		get => _pointsStop.Value;
		set => _pointsStop.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	public BreakoutsWithTimeFilterStrategy()
	{
		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Previous candles the breakout levels span", "Breakout");

		_maLength = Param(nameof(MaLength), 99)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Period of the moving average filter", "Filters");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Require the close on the trade side of the moving average", "Filters");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Allow entries only between 14:30 and 15:00 UTC", "Filters");

		_slType = Param(nameof(SlType), StopLossTypes.Atr)
			.SetDisplay("SL Type", "Stop loss placement method", "Risk");

		_slLength = Param(nameof(SlLength), 0)
			.SetNotNegative()
			.SetDisplay("SL Length", "Earlier candles whose extreme sets a candle stop", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple of the ATR stop", "Risk");

		_pointsStop = Param(nameof(PointsStop), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Points Stop", "Stop distance in price steps", "Risk");

		_riskReward = Param(nameof(RiskReward), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk");

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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ma = new SimpleMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_bars.Clear();
		_stopLevel = 0;
		_targetLevel = 0;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Breakout levels come from the candles before this one.
		var history = _bars.Count >= Length ? _bars.Skip(_bars.Count - Length).ToList() : null;

		_bars.Add((candle.HighPrice, candle.LowPrice));
		var keep = Math.Max(Length, SlLength + 1);
		while (_bars.Count > keep)
			_bars.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _targetLevel)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _targetLevel)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (history == null || !atrValue.IsFormed || (UseMaFilter && !maValue.IsFormed))
			return;

		if (UseTimeFilter)
		{
			var tod = candle.OpenTime.TimeOfDay;
			if (tod < _sessionStart || tod >= _sessionEnd)
				return;
		}

		var close = candle.ClosePrice;
		var highest = history.Max(b => b.high);
		var lowest = history.Min(b => b.low);
		var ma = UseMaFilter ? maValue.GetValue<decimal>() : 0m;
		var atr = atrValue.GetValue<decimal>();

		if (close > highest && (!UseMaFilter || close > ma) && Position <= 0)
		{
			var stop = GetStop(true, close, atr);
			if (stop >= close)
				return;

			_stopLevel = stop;
			_targetLevel = close + RiskReward * (close - stop);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (close < lowest && (!UseMaFilter || close < ma) && Position >= 0)
		{
			var stop = GetStop(false, close, atr);
			if (stop <= close)
				return;

			_stopLevel = stop;
			_targetLevel = close - RiskReward * (stop - close);
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private decimal GetStop(bool isLong, decimal close, decimal atr)
	{
		switch (SlType)
		{
			case StopLossTypes.Atr:
				return isLong ? close - atr * AtrMultiplier : close + atr * AtrMultiplier;

			case StopLossTypes.Candle:
			{
				// The entry candle plus SlLength earlier candles.
				var bars = _bars.Skip(Math.Max(0, _bars.Count - (SlLength + 1))).ToList();
				return isLong ? bars.Min(b => b.low) : bars.Max(b => b.high);
			}

			default:
			{
				var distance = PointsStop * (Security?.PriceStep ?? 1m);
				return isLong ? close - distance : close + distance;
			}
		}
	}
}

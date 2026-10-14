using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NY opening range breakout with MA stop strategy.
/// The 9:30-9:45 (UTC candle open times) range is recorded each day. When the previous candle is the first to close beyond the range
/// high (low) before CutoffHour:CutoffMinute, the current candle enters long (short) if its close is on the same side of the moving
/// average and the side is allowed by TradeDirection. The stop sits at the opposite side of the range; the exit target follows
/// TakeProfitType: TpRatio times the risk, a close back across the moving average, or whichever comes first.
/// </summary>
public class NyOpeningRangeBreakoutMaStopStrategy : Strategy
{
	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Long entries only.
		/// </summary>
		LongOnly,

		/// <summary>
		/// Short entries only.
		/// </summary>
		ShortOnly,

		/// <summary>
		/// Long and short entries.
		/// </summary>
		Both,
	}

	/// <summary>
	/// Take profit modes.
	/// </summary>
	public enum TakeProfitTypes
	{
		/// <summary>
		/// Fixed reward-to-risk target.
		/// </summary>
		FixedRiskReward,

		/// <summary>
		/// Exit when the close crosses the moving average.
		/// </summary>
		MaCross,

		/// <summary>
		/// Whichever of the two exits comes first.
		/// </summary>
		Both,
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		SMA,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		EMA,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		SMMA,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		WMA,
	}

	private static readonly TimeSpan _rangeStart = new(9, 30, 0);
	private static readonly TimeSpan _rangeEnd = new(9, 45, 0);

	private readonly StrategyParam<int> _cutoffHour;
	private readonly StrategyParam<int> _cutoffMinute;
	private readonly StrategyParam<TradeDirections> _tradeDirection;
	private readonly StrategyParam<TakeProfitTypes> _takeProfitType;
	private readonly StrategyParam<decimal> _tpRatio;
	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _prevClose;
	private decimal? _prevPrevClose;
	private bool _prevBeforeCutoff;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Hour after which no new breakouts are taken.
	/// </summary>
	public int CutoffHour
	{
		get => _cutoffHour.Value;
		set => _cutoffHour.Value = value;
	}

	/// <summary>
	/// Minute of the cutoff time.
	/// </summary>
	public int CutoffMinute
	{
		get => _cutoffMinute.Value;
		set => _cutoffMinute.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirections TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Take profit mode.
	/// </summary>
	public TakeProfitTypes TakeProfitType
	{
		get => _takeProfitType.Value;
		set => _takeProfitType.Value = value;
	}

	/// <summary>
	/// Reward-to-risk ratio of the fixed target.
	/// </summary>
	public decimal TpRatio
	{
		get => _tpRatio.Value;
		set => _tpRatio.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	public NyOpeningRangeBreakoutMaStopStrategy()
	{
		_cutoffHour = Param(nameof(CutoffHour), 12)
			.SetRange(0, 23)
			.SetDisplay("Cutoff Hour", "Hour after which no new breakouts are taken", "Session");

		_cutoffMinute = Param(nameof(CutoffMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Cutoff Minute", "Minute of the cutoff time", "Session");

		_tradeDirection = Param(nameof(TradeDirection), TradeDirections.LongOnly)
			.SetDisplay("Trade Direction", "Allowed trade direction", "Trading");

		_takeProfitType = Param(nameof(TakeProfitType), TakeProfitTypes.FixedRiskReward)
			.SetDisplay("Take Profit Type", "Fixed reward-to-risk, MA cross or both", "Risk");

		_tpRatio = Param(nameof(TpRatio), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP Ratio", "Reward-to-risk ratio of the fixed target", "Risk");

		_maType = Param(nameof(MaType), MaTypes.SMA)
			.SetDisplay("MA Type", "Moving average type", "Moving Average");

		_maLength = Param(nameof(MaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average");

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
		_currentDay = default;
		_rangeHigh = null;
		_rangeLow = null;
		_prevClose = null;
		_prevPrevClose = null;
		_prevBeforeCutoff = false;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		DecimalLengthIndicator ma = MaType switch
		{
			MaTypes.EMA => new ExponentialMovingAverage(),
			MaTypes.SMMA => new SmoothedMovingAverage(),
			MaTypes.WMA => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};
		ma.Length = MaLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		var tod = candle.OpenTime.TimeOfDay;

		if (day != _currentDay)
		{
			_currentDay = day;
			_rangeHigh = null;
			_rangeLow = null;
			_prevClose = null;
			_prevPrevClose = null;
			_prevBeforeCutoff = false;
		}

		var close = candle.ClosePrice;

		if (tod >= _rangeStart && tod < _rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			return;
		}

		var prevClose = _prevClose;
		var prevPrevClose = _prevPrevClose;
		var prevBeforeCutoff = _prevBeforeCutoff;
		var cutoff = new TimeSpan(CutoffHour, CutoffMinute, 0);

		if (tod >= _rangeEnd)
		{
			_prevPrevClose = _prevClose;
			_prevClose = close;
			_prevBeforeCutoff = tod < cutoff;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var useFixed = TakeProfitType != TakeProfitTypes.MaCross;
		var useMa = TakeProfitType != TakeProfitTypes.FixedRiskReward;

		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop)
				|| (useFixed && _takePrice is decimal take && candle.HighPrice >= take)
				|| (useMa && close < maValue))
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop)
				|| (useFixed && _takePrice is decimal take && candle.LowPrice <= take)
				|| (useMa && close > maValue))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		if (_rangeHigh is not decimal high || _rangeLow is not decimal low || prevClose is not decimal pc || !prevBeforeCutoff)
			return;

		// The previous candle must be the first close beyond the range.
		var brokeUp = pc > high && (prevPrevClose is not decimal ppc || ppc <= high);
		var brokeDown = pc < low && (prevPrevClose is not decimal ppc2 || ppc2 >= low);

		var allowLong = TradeDirection != TradeDirections.ShortOnly;
		var allowShort = TradeDirection != TradeDirections.LongOnly;

		if (brokeUp && allowLong && close > maValue && close > low && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = low;
			_takePrice = close + (close - low) * TpRatio;
		}
		else if (brokeDown && allowShort && close < maValue && close < high && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = high;
			_takePrice = close - (high - close) * TpRatio;
		}
	}
}

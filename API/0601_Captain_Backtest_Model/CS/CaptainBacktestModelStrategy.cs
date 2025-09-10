namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Captain Backtest Model strategy.
/// Determines bias from a morning price range and trades breakouts after retracements.
/// </summary>
public class CaptainBacktestModelStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _prevRangeStart;
	private readonly StrategyParam<TimeSpan> _prevRangeEnd;
	private readonly StrategyParam<TimeSpan> _takeStart;
	private readonly StrategyParam<TimeSpan> _takeEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<bool> _waitOppClose;
	private readonly StrategyParam<bool> _waitRetraceHl;
	private readonly StrategyParam<bool> _useStopOrders;
	private readonly StrategyParam<bool> _useFixedRr;
	private readonly StrategyParam<decimal> _riskPoints;
	private readonly StrategyParam<decimal> _rewardPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _preHigh;
	private decimal? _preLow;
	private bool? _bias;
	private bool _oppClose;
	private bool _tookHl;
	private bool _longEntered;
	private bool _shortEntered;
	private bool _prevTrade;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool? _prevBias;

	/// <summary>
	/// Start of the range to track.
	/// </summary>
	public TimeSpan PrevRangeStart { get => _prevRangeStart.Value; set => _prevRangeStart.Value = value; }

	/// <summary>
	/// End of the range to track.
	/// </summary>
	public TimeSpan PrevRangeEnd { get => _prevRangeEnd.Value; set => _prevRangeEnd.Value = value; }

	/// <summary>
	/// Start of bias determination window.
	/// </summary>
	public TimeSpan TakeStart { get => _takeStart.Value; set => _takeStart.Value = value; }

	/// <summary>
	/// End of bias determination window.
	/// </summary>
	public TimeSpan TakeEnd { get => _takeEnd.Value; set => _takeEnd.Value = value; }

	/// <summary>
	/// Start of trade window.
	/// </summary>
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }

	/// <summary>
	/// End of trade window.
	/// </summary>
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }

	/// <summary>
	/// Require opposite close retracement.
	/// </summary>
	public bool WaitOppositeClose { get => _waitOppClose.Value; set => _waitOppClose.Value = value; }

	/// <summary>
	/// Require taking previous high/low.
	/// </summary>
	public bool WaitRetraceHl { get => _waitRetraceHl.Value; set => _waitRetraceHl.Value = value; }

	/// <summary>
	/// Enter with stop orders instead of market orders.
	/// </summary>
	public bool UseStopOrders { get => _useStopOrders.Value; set => _useStopOrders.Value = value; }

	/// <summary>
	/// Use fixed risk/reward exits.
	/// </summary>
	public bool UseFixedRr { get => _useFixedRr.Value; set => _useFixedRr.Value = value; }

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public decimal Risk { get => _riskPoints.Value; set => _riskPoints.Value = value; }

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public decimal Reward { get => _rewardPoints.Value; set => _rewardPoints.Value = value; }

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="CaptainBacktestModelStrategy"/>.
	/// </summary>
	public CaptainBacktestModelStrategy()
	{
		_prevRangeStart = Param(nameof(PrevRangeStart), TimeSpan.FromHours(6))
			.SetDisplay("Prev Range Start", "Range start", "Time");
		_prevRangeEnd = Param(nameof(PrevRangeEnd), TimeSpan.FromHours(10))
			.SetDisplay("Prev Range End", "Range end", "Time");
		_takeStart = Param(nameof(TakeStart), TimeSpan.FromHours(10))
			.SetDisplay("Take Start", "Bias start", "Time");
		_takeEnd = Param(nameof(TakeEnd), new TimeSpan(11, 15, 0))
			.SetDisplay("Take End", "Bias end", "Time");
		_tradeStart = Param(nameof(TradeStart), TimeSpan.FromHours(10))
			.SetDisplay("Trade Start", "Trade start", "Time");
		_tradeEnd = Param(nameof(TradeEnd), TimeSpan.FromHours(16))
			.SetDisplay("Trade End", "Trade end", "Time");
		_waitOppClose = Param(nameof(WaitOppositeClose), true)
			.SetDisplay("Wait Opposite Close", "Require opposite close", "Strategy");
		_waitRetraceHl = Param(nameof(WaitRetraceHl), true)
			.SetDisplay("Wait Took HL", "Require previous high/low", "Strategy");
		_useStopOrders = Param(nameof(UseStopOrders), false)
			.SetDisplay("Use Stop Orders", "Use stop entries", "Strategy");
		_useFixedRr = Param(nameof(UseFixedRr), true)
			.SetDisplay("Use Fixed R:R", "Enable fixed risk/reward", "Risk");
		_riskPoints = Param(nameof(Risk), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Stop-loss points", "Risk");
		_rewardPoints = Param(nameof(Reward), 75m)
			.SetGreaterThanZero()
			.SetDisplay("Reward", "Take-profit points", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_preHigh = null;
		_preLow = null;
		_bias = null;
		_oppClose = false;
		_tookHl = false;
		_longEntered = false;
		_shortEntered = false;
		_prevTrade = false;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevBias = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: UseFixedRr ? new Unit(Reward, UnitTypes.Absolute) : new Unit(0, UnitTypes.Absolute),
			stopLoss: UseFixedRr ? new Unit(Risk, UnitTypes.Absolute) : new Unit(0, UnitTypes.Absolute)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private static bool InSession(TimeSpan time, TimeSpan start, TimeSpan end) => time >= start && time < end;

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;
		var tPre = InSession(time, PrevRangeStart, PrevRangeEnd);
		var tTake = InSession(time, TakeStart, TakeEnd);
		var tTrade = InSession(time, TradeStart, TradeEnd);

		if (tPre)
		{
			_preHigh = _preHigh is null ? candle.HighPrice : Math.Max(_preHigh.Value, candle.HighPrice);
			_preLow = _preLow is null ? candle.LowPrice : Math.Min(_preLow.Value, candle.LowPrice);
		}

		if (!tTrade && _prevTrade)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			_preHigh = null;
			_preLow = null;
			_bias = null;
			_oppClose = false;
			_tookHl = false;
			_longEntered = false;
			_shortEntered = false;
		}

		var prevBias = _bias;

		if (tTake && _bias is null && _preHigh is not null && _preLow is not null)
		{
			if (candle.HighPrice > _preHigh)
				_bias = true;
			else if (candle.LowPrice < _preLow)
				_bias = false;
		}

		if (tTrade)
		{
			if (!WaitOppositeClose ||
				(_bias == true && candle.ClosePrice < candle.OpenPrice) ||
				(_bias == false && candle.ClosePrice > candle.OpenPrice))
				_oppClose = true;

			if (!WaitRetraceHl ||
				(_bias == true && candle.LowPrice < _prevLow) ||
				(_bias == false && candle.HighPrice > _prevHigh))
				_tookHl = true;

			if (prevBias == true && _bias == true && candle.ClosePrice > _prevHigh && _oppClose && _tookHl && !_longEntered)
			{
				_longEntered = true;
				if (UseStopOrders)
					BuyStop(Volume + Math.Abs(Position), candle.HighPrice);
				else
					BuyMarket(Volume + Math.Abs(Position));
			}
			else if (prevBias == false && _bias == false && candle.ClosePrice < _prevLow && _oppClose && _tookHl && !_shortEntered)
			{
				_shortEntered = true;
				if (UseStopOrders)
					SellStop(Volume + Math.Abs(Position), candle.LowPrice);
				else
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevTrade = tTrade;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevBias = _bias;
	}
}


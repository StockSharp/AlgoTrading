using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion of the WTI-Brent price differential. <see cref="Strategy.Security"/> is the front-month
/// WTI contract and <see cref="BrentSecurity"/> the front-month Brent contract; the spread is the WTI close
/// minus the Brent close of the bar both legs finished. When the spread's z-score over <see cref="Lookback"/>
/// bars moves beyond <see cref="EntryZScore"/>, the grade that is cheap against the spread's average is
/// bought and the expensive one sold for the same dollar amount. The pair is closed when the spread returns
/// to its average, when it widens <see cref="StopWidening"/> standard deviations past its level at entry,
/// or <see cref="RollDays"/> days before the earlier expiry of the two contracts.
/// </summary>
public class WTIBrentSpreadStrategy : Strategy
{
	private const string _entryComment = "Spread entry";
	private const string _averageComment = "Spread at average";
	private const string _stopComment = "Spread stop";
	private const string _rollComment = "Contract roll";

	private readonly StrategyParam<Security> _brentSecurity;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _entryZScore;
	private readonly StrategyParam<decimal> _stopWidening;
	private readonly StrategyParam<int> _rollDays;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _spreadAverage;
	private StandardDeviation _spreadDeviation;
	private DateTime? _wtiBarTime;
	private decimal _wtiClose;
	private DateTime? _brentBarTime;
	private decimal _brentClose;
	private DateTime? _pairTime;
	private decimal? _stopSpread;
	private int _blockedSign;
	private Order _wtiOrder;
	private Order _brentOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="WTIBrentSpreadStrategy"/>.
	/// </summary>
	public WTIBrentSpreadStrategy()
	{
		_brentSecurity = Param<Security>(nameof(BrentSecurity))
			.SetDisplay("Brent Security", "Front-month Brent contract traded against the WTI security", "Instruments")
			.SetRequired();

		_lookback = Param(nameof(Lookback), 20)
			.SetRange(2, int.MaxValue)
			.SetDisplay("Lookback", "Bars that define the spread's average and standard deviation", "Spread")
			.SetOptimize(10, 60, 10);

		_entryZScore = Param(nameof(EntryZScore), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Z-Score", "Standard deviations from the average beyond which the pair is opened", "Spread")
			.SetOptimize(1.5m, 3m, 0.5m);

		_stopWidening = Param(nameof(StopWidening), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Widening", "Standard deviations the spread may widen past its level at entry before the pair is closed", "Risk")
			.SetOptimize(0.5m, 3m, 0.5m);

		_rollDays = Param(nameof(RollDays), 5)
			.SetNotNegative()
			.SetDisplay("Roll Days", "Days before the earlier front-month expiry when the pair is closed", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Bars on which the two legs are compared", "General");
	}

	/// <summary>
	/// Front-month Brent contract traded against the WTI contract in <see cref="Strategy.Security"/>.
	/// </summary>
	public Security BrentSecurity
	{
		get => _brentSecurity.Value;
		set => _brentSecurity.Value = value;
	}

	/// <summary>
	/// Number of bars, at least two, that define the spread's average and standard deviation.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Distance from the spread's average, in standard deviations, beyond which the pair is opened.
	/// </summary>
	public decimal EntryZScore
	{
		get => _entryZScore.Value;
		set => _entryZScore.Value = value;
	}

	/// <summary>
	/// Distance, in standard deviations at entry, the spread may widen past its level at entry before the pair is closed.
	/// </summary>
	public decimal StopWidening
	{
		get => _stopWidening.Value;
		set => _stopWidening.Value = value;
	}

	/// <summary>
	/// Days before the earlier front-month expiry of the two legs when the pair is closed and no new one is opened.
	/// </summary>
	public int RollDays
	{
		get => _rollDays.Value;
		set => _rollDays.Value = value;
	}

	/// <summary>
	/// Type of the candles on which the two legs are compared.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (BrentSecurity != null)
			yield return (BrentSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_spreadAverage = null;
		_spreadDeviation = null;
		ClearState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Checked before any subscription: a missing Brent leg would otherwise fall back to the WTI security.
		if (Security is null)
			throw new InvalidOperationException("Security must be the front-month WTI contract.");

		if (BrentSecurity is null || BrentSecurity.Id.EqualsIgnoreCase(Security.Id))
			throw new InvalidOperationException("BrentSecurity must be a different instrument than the WTI security.");

		base.OnStarted2(time);

		ClearState();
		_spreadAverage = new SimpleMovingAverage { Length = Lookback };
		_spreadDeviation = new StandardDeviation { Length = Lookback };

		var wtiSubscription = SubscribeCandles(CandleType);
		wtiSubscription
			.Bind(ProcessWtiCandle)
			.Start();

		SubscribeCandles(CandleType, security: BrentSecurity)
			.Bind(ProcessBrentCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, wtiSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ClearState()
	{
		_wtiBarTime = null;
		_wtiClose = 0m;
		_brentBarTime = null;
		_brentClose = 0m;
		_pairTime = null;
		_stopSpread = null;
		_blockedSign = 0;
		_wtiOrder = null;
		_brentOrder = null;
	}

	private void ProcessWtiCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _wtiBarTime is DateTime last && candle.OpenTime <= last)
			return;

		_wtiBarTime = candle.OpenTime;
		_wtiClose = candle.ClosePrice;
		ProcessPair();
	}

	private void ProcessBrentCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _brentBarTime is DateTime last && candle.OpenTime <= last)
			return;

		_brentBarTime = candle.OpenTime;
		_brentClose = candle.ClosePrice;
		ProcessPair();
	}

	private void ProcessPair()
	{
		// Only bars both legs finished at the same time form a spread; an unmatched bar is never reused.
		if (_wtiBarTime is not DateTime barTime || _brentBarTime != barTime || _pairTime == barTime)
			return;

		_pairTime = barTime;

		var spread = _wtiClose - _brentClose;
		var averageValue = _spreadAverage.Process(spread, barTime, true);
		var deviationValue = _spreadDeviation.Process(spread, barTime, true);

		if (!_spreadAverage.IsFormed || !_spreadDeviation.IsFormed)
			return;

		var average = averageValue.GetValue<decimal>();
		var deviation = deviationValue.GetValue<decimal>();
		// A window of equal spreads has no deviation: the spread sits at its average.
		var zScore = deviation == 0m ? 0m : (spread - average) / deviation;

		// After a stop the same side waits until the spread is back inside the entry threshold.
		if (_blockedSign != 0 && _blockedSign * zScore <= EntryZScore)
			_blockedSign = 0;

		if (IsPending(_wtiOrder) || IsPending(_brentOrder))
			return;

		var wtiPosition = Position;
		var brentPosition = GetPositionValue(BrentSecurity, Portfolio) ?? 0m;
		var now = CurrentTime;

		if (wtiPosition != 0m || brentPosition != 0m)
		{
			if (!IsFormedAndOnlineAndAllowTrading(StrategyTradingModes.ReducePositionOnly))
				return;

			// 1 holds long WTI and short Brent, -1 the opposite.
			var side = wtiPosition != 0m ? Math.Sign(wtiPosition) : -Math.Sign(brentPosition);
			var reason = GetExitReason(side, spread, zScore, now);

			if (reason is null)
				return;

			if (reason == _stopComment)
				_blockedSign = -side;

			ClosePair(wtiPosition, brentPosition, reason);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || IsRollDue(now))
			return;

		// 1 buys WTI while it is cheap against Brent, -1 sells it while it is expensive.
		int direction;

		if (zScore > EntryZScore)
			direction = -1;
		else if (zScore < -EntryZScore)
			direction = 1;
		else
			return;

		if (_blockedSign == -direction)
			return;

		var brentVolume = GetBrentVolume(_wtiClose, _brentClose);

		if (brentVolume <= 0m)
		{
			LogWarning($"No Brent volume within the instrument limits matches the value of {Volume} WTI at {_wtiClose} against Brent at {_brentClose}.");
			return;
		}

		// Fixed at entry, past the entry spread on the side it would keep widening to.
		_stopSpread = spread - direction * StopWidening * deviation;
		_wtiOrder = RegisterLeg(Security, direction > 0 ? Sides.Buy : Sides.Sell, Volume, _entryComment);
		_brentOrder = RegisterLeg(BrentSecurity, direction > 0 ? Sides.Sell : Sides.Buy, brentVolume, _entryComment);
	}

	private string GetExitReason(int side, decimal spread, decimal zScore, DateTime time)
	{
		if (IsRollDue(time))
			return _rollComment;

		if (_stopSpread is decimal stop && (side > 0 ? spread <= stop : spread >= stop))
			return _stopComment;

		return (side > 0 ? zScore >= 0m : zScore <= 0m) ? _averageComment : null;
	}

	private bool IsRollDue(DateTime time)
	{
		var expiry = Security.ExpiryDate;

		if (BrentSecurity.ExpiryDate is DateTime brentExpiry && (expiry is null || brentExpiry < expiry))
			expiry = brentExpiry;

		return expiry is DateTime date && time >= date.AddDays(-RollDays);
	}

	private decimal GetBrentVolume(decimal wtiPrice, decimal brentPrice)
	{
		// The Brent quantity worth as many dollars as Volume WTI contracts, to the nearest volume step.
		var wtiValue = Volume * wtiPrice * GetLotSize(Security);
		var brentLotValue = brentPrice * GetLotSize(BrentSecurity);

		if (wtiValue <= 0m || brentLotValue <= 0m)
			return 0m;

		var step = BrentSecurity.VolumeStep is decimal volumeStep && volumeStep > 0m ? volumeStep : 1m;
		var volume = Math.Round(wtiValue / brentLotValue / step, MidpointRounding.AwayFromZero) * step;

		if (BrentSecurity.MinVolume is decimal min && volume < min)
			return 0m;

		if (BrentSecurity.MaxVolume is decimal max && max > 0m && volume > max)
			return 0m;

		return volume;
	}

	private void ClosePair(decimal wtiPosition, decimal brentPosition, string reason)
	{
		if (wtiPosition != 0m)
			_wtiOrder = RegisterLeg(Security, wtiPosition > 0m ? Sides.Sell : Sides.Buy, Math.Abs(wtiPosition), reason);

		if (brentPosition != 0m)
			_brentOrder = RegisterLeg(BrentSecurity, brentPosition > 0m ? Sides.Sell : Sides.Buy, Math.Abs(brentPosition), reason);

		_stopSpread = null;
	}

	private Order RegisterLeg(Security security, Sides side, decimal volume, string comment)
	{
		var order = CreateOrder(side, 0m, volume, security);
		order.Comment = comment;
		RegisterOrder(order);
		return order;
	}

	private static decimal GetLotSize(Security security)
		=> security.Multiplier is decimal multiplier && multiplier > 0m ? multiplier : 1m;

	private static bool IsPending(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);
}

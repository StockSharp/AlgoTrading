using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Captures the spread between the synthetic lending rate implied by perpetual swap funding and an
/// on-chain lending yield: it lends in the venue that pays more, borrows in the one that pays less
/// and keeps the two legs dollar-neutral.
/// </summary>
/// <remarks>
/// <see cref="Strategy.Security"/> is the derivative market's leg and <see cref="OnChainLegSecurity"/> the
/// DeFi platform's; buying a leg lends in its venue and selling it borrows there. Each rate counts with the
/// latest close of its series, whenever that series last printed, and the pair is judged once per period of
/// the legs. The spread thresholds are in the units of the two rate series; the defaults assume both are
/// quoted as annualized percent.
/// </remarks>
public class SyntheticLendingRatesStrategy : Strategy
{
	[Flags]
	private enum StreamRoles
	{
		None = 0,
		FundingRate = 1 << 0,
		LendingRate = 1 << 1,
		DerivativeLeg = 1 << 2,
		OnChainLeg = 1 << 3,
	}

	private readonly StrategyParam<Security> _fundingRateSecurity;
	private readonly StrategyParam<Security> _lendingRateSecurity;
	private readonly StrategyParam<Security> _onChainLegSecurity;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _spreadCap;
	private readonly StrategyParam<decimal> _minLegTurnover;
	private readonly StrategyParam<decimal> _legNotional;
	private readonly StrategyParam<int> _rebalanceBars;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _fundingBar;
	private ICandleMessage _lendingBar;
	private ICandleMessage _derivativeBar;
	private ICandleMessage _onChainBar;
	private DateTime? _period;
	private int _barsSinceSizing;

	/// <summary>
	/// Initializes a new instance of the <see cref="SyntheticLendingRatesStrategy"/>.
	/// </summary>
	public SyntheticLendingRatesStrategy()
	{
		_fundingRateSecurity = Param<Security>(nameof(FundingRateSecurity))
			.SetDisplay("Funding Rate", "Synthetic lending rate implied by perpetual swap funding", "Rates")
			.SetRequired();

		_lendingRateSecurity = Param<Security>(nameof(LendingRateSecurity))
			.SetDisplay("Lending Rate", "Lending yield of the DeFi platform", "Rates")
			.SetRequired();

		_onChainLegSecurity = Param<Security>(nameof(OnChainLegSecurity))
			.SetDisplay("On-Chain Leg", "Instrument bought to lend and sold to borrow on the DeFi platform", "Legs")
			.SetRequired();

		_entryThreshold = Param(nameof(EntryThreshold), 5m)
			.SetNotNegative()
			.SetDisplay("Entry Threshold", "Rate spread above which the pair is opened", "Spread");

		_exitThreshold = Param(nameof(ExitThreshold), 1m)
			.SetDisplay("Exit Threshold", "Spread in favour of the open pair at or below which it has reverted", "Spread");

		_spreadCap = Param(nameof(SpreadCap), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Spread Cap", "Spread at or above which the pair is closed and none is opened", "Risk");

		_minLegTurnover = Param(nameof(MinLegTurnover), 1_000_000m)
			.SetNotNegative()
			.SetDisplay("Min Leg Turnover", "Value each leg must trade in a bar; below it the liquidity stop closes the pair", "Risk");

		_legNotional = Param(nameof(LegNotional), 10_000m)
			.SetGreaterThanZero()
			.SetDisplay("Leg Notional", "Value held long and short in each leg", "Trading");

		_rebalanceBars = Param(nameof(RebalanceBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("Rebalance Bars", "Bars between resizing both legs back to the leg notional", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame bars sampled from both rate series and both legs", "General");
	}

	/// <summary>
	/// Rate series of the synthetic lending rate derived from perpetual swap funding; its latest close is the rate.
	/// </summary>
	public Security FundingRateSecurity
	{
		get => _fundingRateSecurity.Value;
		set => _fundingRateSecurity.Value = value;
	}

	/// <summary>
	/// Rate series of the on-chain lending yield; its latest close is the rate.
	/// </summary>
	public Security LendingRateSecurity
	{
		get => _lendingRateSecurity.Value;
		set => _lendingRateSecurity.Value = value;
	}

	/// <summary>
	/// Instrument that lends on the DeFi platform when bought and borrows there when sold. The derivative
	/// market's leg is <see cref="Strategy.Security"/>.
	/// </summary>
	public Security OnChainLegSecurity
	{
		get => _onChainLegSecurity.Value;
		set => _onChainLegSecurity.Value = value;
	}

	/// <summary>
	/// Spread between the two rates above which a pair is opened.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Spread in favour of the open pair at or below which the spread has reverted and the pair is closed.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Spread at or above which the open pair is closed and no new pair is opened.
	/// </summary>
	public decimal SpreadCap
	{
		get => _spreadCap.Value;
		set => _spreadCap.Value = value;
	}

	/// <summary>
	/// Value each leg must trade within a bar: volume times close times the leg's lot size. Below it on either
	/// leg no pair is opened and an open pair is closed; a leg with no bar in the period traded nothing.
	/// </summary>
	public decimal MinLegTurnover
	{
		get => _minLegTurnover.Value;
		set => _minLegTurnover.Value = value;
	}

	/// <summary>
	/// Value held in each leg (volume times price times lot size), long in the lending venue and short in the
	/// borrowing one.
	/// </summary>
	public decimal LegNotional
	{
		get => _legNotional.Value;
		set => _legNotional.Value = value;
	}

	/// <summary>
	/// Number of bars an open pair is held before both legs are resized back to <see cref="LegNotional"/>.
	/// </summary>
	public int RebalanceBars
	{
		get => _rebalanceBars.Value;
		set => _rebalanceBars.Value = value;
	}

	/// <summary>
	/// Time-frame bars sampled from both rate series and both legs.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> GetStreams().Select(security => (security, CandleType));

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fundingBar = null;
		_lendingBar = null;
		_derivativeBar = null;
		_onChainBar = null;
		_period = null;
		_barsSinceSizing = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Reject before subscribing: a missing instrument would silently fall back to Security.
		ValidateInputs();

		base.OnStarted2(time);

		ISubscriptionHandler<ICandleMessage> derivativeSubscription = null;

		// One subscription per instrument, even when it plays several roles. Unfinished bars are taken too:
		// a bar built from trades is marked finished only by the instrument's next trade, however late.
		foreach (var stream in GetStreams())
		{
			var roles = GetRoles(stream);
			var subscription = SubscribeCandles(CandleType, false, stream);
			subscription.Bind(candle => ProcessCandle(roles, candle)).Start();

			if (roles.HasFlag(StreamRoles.DerivativeLeg))
				derivativeSubscription = subscription;
		}

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, derivativeSubscription);
			DrawOwnTrades(area);
		}
	}

	private static decimal RoundDown(decimal volume, Security security)
		=> security.VolumeStep is decimal step && step > 0m ? Math.Floor(volume / step) * step : volume;

	private static decimal GetLotSize(Security security)
		=> security.Multiplier is decimal multiplier && multiplier > 0m ? multiplier : 1m;

	// A leg without a bar of the period traded nothing in it.
	private static decimal GetTradedValue(ICandleMessage bar, Security leg, DateTime period)
		=> bar is not null && bar.OpenTime == period ? bar.TotalVolume * bar.ClosePrice * GetLotSize(leg) : 0m;

	private static bool IsSame(Security left, Security right)
		=> left != null && right != null && left.Id.EqualsIgnoreCase(right.Id);

	private void ValidateInputs()
	{
		if (Security is null)
			throw new InvalidOperationException("Security must be set to the derivative market's leg.");

		if (FundingRateSecurity is null)
			throw new InvalidOperationException($"{nameof(FundingRateSecurity)} must be set.");

		if (LendingRateSecurity is null)
			throw new InvalidOperationException($"{nameof(LendingRateSecurity)} must be set.");

		if (OnChainLegSecurity is null)
			throw new InvalidOperationException($"{nameof(OnChainLegSecurity)} must be set.");

		if (IsSame(FundingRateSecurity, LendingRateSecurity))
			throw new InvalidOperationException($"{nameof(LendingRateSecurity)} must be a different rate series than {nameof(FundingRateSecurity)}.");

		if (IsSame(OnChainLegSecurity, Security))
			throw new InvalidOperationException($"{nameof(OnChainLegSecurity)} must be a different instrument than Security.");

		if (CandleType is not { IsTFCandles: true })
			throw new InvalidOperationException($"{nameof(CandleType)} must be a time-frame candle type, so that all four streams share periods.");

		if (ExitThreshold >= EntryThreshold)
			throw new InvalidOperationException($"{nameof(ExitThreshold)} must be below {nameof(EntryThreshold)}.");

		if (SpreadCap <= EntryThreshold)
			throw new InvalidOperationException($"{nameof(SpreadCap)} must be above {nameof(EntryThreshold)}.");
	}

	private void ProcessCandle(StreamRoles roles, ICandleMessage candle)
	{
		// Streams arrive in time order, so the first leg bar of a later period means every stream has delivered
		// all of the pending one. Periods are set by the legs, whatever the cadence of the rate series.
		if ((roles & (StreamRoles.DerivativeLeg | StreamRoles.OnChainLeg)) != StreamRoles.None && (_period is null || candle.OpenTime > _period))
		{
			if (_period is DateTime pending)
				Decide(pending);

			_period = candle.OpenTime;
		}

		if (roles.HasFlag(StreamRoles.FundingRate))
			_fundingBar = candle;

		if (roles.HasFlag(StreamRoles.LendingRate))
			_lendingBar = candle;

		if (roles.HasFlag(StreamRoles.DerivativeLeg))
			_derivativeBar = candle;

		if (roles.HasFlag(StreamRoles.OnChainLeg))
			_onChainBar = candle;
	}

	private void Decide(DateTime period)
	{
		// No spread exists until both rate series have printed.
		if (_fundingBar is null || _lendingBar is null)
			return;

		// Closing needs only the right to reduce positions; nothing is decided while an order is still working.
		if (!IsFormedAndOnlineAndAllowTrading(StrategyTradingModes.ReducePositionOnly) || HasWorkingOrders())
			return;

		var spread = _fundingBar.ClosePrice - _lendingBar.ClosePrice;
		var isLiquid = GetTradedValue(_derivativeBar, Security, period) >= MinLegTurnover
			&& GetTradedValue(_onChainBar, OnChainLegSecurity, period) >= MinLegTurnover;

		var derivativePosition = GetLegPosition(Security);
		var onChainPosition = GetLegPosition(OnChainLegSecurity);

		if (derivativePosition == 0m && onChainPosition == 0m)
		{
			var opens = isLiquid && Math.Abs(spread) > EntryThreshold && Math.Abs(spread) < SpreadCap && IsFormedAndOnlineAndAllowTrading();

			if (opens && ResizeLegs(Math.Sign(spread), 0m, 0m))
				_barsSinceSizing = 0;

			return;
		}

		// +1 while the derivative leg lends (long) and the on-chain leg borrows (short), -1 the other way round.
		var lendingSide = derivativePosition != 0m ? Math.Sign(derivativePosition) : -Math.Sign(onChainPosition);

		if (lendingSide * spread <= ExitThreshold || Math.Abs(spread) >= SpreadCap || !isLiquid)
		{
			Flatten(Security, derivativePosition);
			Flatten(OnChainLegSecurity, onChainPosition);
			return;
		}

		// Resizing may enlarge a leg, so it waits for full trading rights.
		if (++_barsSinceSizing < RebalanceBars || !IsFormedAndOnlineAndAllowTrading())
			return;

		_barsSinceSizing = 0;
		ResizeLegs(lendingSide, derivativePosition, onChainPosition);
	}

	private bool ResizeLegs(int lendingSide, decimal derivativePosition, decimal onChainPosition)
	{
		var derivativeUnits = ToUnits(Security, _derivativeBar);
		var onChainUnits = ToUnits(OnChainLegSecurity, _onChainBar);

		// A leg that cannot be sized would leave the pair unhedged, so neither leg moves.
		if (derivativeUnits <= 0m || onChainUnits <= 0m)
			return false;

		MoveTo(Security, lendingSide * derivativeUnits, derivativePosition);
		MoveTo(OnChainLegSecurity, -lendingSide * onChainUnits, onChainPosition);
		return true;
	}

	private decimal ToUnits(Security leg, ICandleMessage bar)
	{
		// Sized at the leg's last traded price, in lots of the leg's lot size.
		var lotValue = bar is null ? 0m : bar.ClosePrice * GetLotSize(leg);

		if (lotValue <= 0m)
			return 0m;

		var units = RoundDown(LegNotional / lotValue, leg);
		return units >= (leg.MinVolume ?? 0m) ? units : 0m;
	}

	private void MoveTo(Security security, decimal target, decimal position)
	{
		var difference = target - position;
		var volume = RoundDown(Math.Abs(difference), security);

		if (volume <= 0m || volume < (security.MinVolume ?? 0m))
			return;

		if (difference > 0m)
			BuyMarket(volume, security);
		else
			SellMarket(volume, security);
	}

	private void Flatten(Security security, decimal position)
	{
		if (position > 0m)
			SellMarket(position, security);
		else if (position < 0m)
			BuyMarket(-position, security);
	}

	private decimal GetLegPosition(Security leg)
		=> GetPositionValue(leg, Portfolio) ?? 0m;

	private bool HasWorkingOrders()
		=> Orders.Any(order => order.State is not (OrderStates.Done or OrderStates.Failed));

	private StreamRoles GetRoles(Security stream)
	{
		var roles = StreamRoles.None;

		if (IsSame(stream, FundingRateSecurity))
			roles |= StreamRoles.FundingRate;

		if (IsSame(stream, LendingRateSecurity))
			roles |= StreamRoles.LendingRate;

		if (IsSame(stream, Security))
			roles |= StreamRoles.DerivativeLeg;

		if (IsSame(stream, OnChainLegSecurity))
			roles |= StreamRoles.OnChainLeg;

		return roles;
	}

	private IEnumerable<Security> GetStreams()
	{
		Security[] streams = [FundingRateSecurity, LendingRateSecurity, Security, OnChainLegSecurity];

		return streams
			.Where(security => security != null)
			.DistinctBy(security => security.Id, StringComparer.OrdinalIgnoreCase);
	}
}

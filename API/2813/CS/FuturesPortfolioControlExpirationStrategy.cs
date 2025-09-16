using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Controls a three-leg futures portfolio and rolls contracts before expiration.
/// Finds the current contract for each futures family and keeps the desired exposure active.
/// </summary>
public class FuturesPortfolioControlExpirationStrategy : Strategy
{
	private readonly StrategyParam<string> _boardCode;
	private readonly StrategyParam<string> _symbol1;
	private readonly StrategyParam<string> _symbol2;
	private readonly StrategyParam<string> _symbol3;
	private readonly StrategyParam<int> _lot1;
	private readonly StrategyParam<int> _lot2;
	private readonly StrategyParam<int> _lot3;
	private readonly StrategyParam<int> _hoursBeforeExpiration;
	private readonly StrategyParam<DataType> _monitoringCandleType;

	private readonly LegState[] _legs;

	/// <summary>
	/// Exchange board code appended to futures identifiers.
	/// </summary>
	public string BoardCode
	{
		get => _boardCode.Value;
		set => _boardCode.Value = value;
	}

	/// <summary>
	/// Short code of the first futures family (for example MXI or BR).
	/// </summary>
	public string Symbol1
	{
		get => _symbol1.Value;
		set => _symbol1.Value = value;
	}

	/// <summary>
	/// Short code of the second futures family.
	/// </summary>
	public string Symbol2
	{
		get => _symbol2.Value;
		set => _symbol2.Value = value;
	}

	/// <summary>
	/// Short code of the third futures family.
	/// </summary>
	public string Symbol3
	{
		get => _symbol3.Value;
		set => _symbol3.Value = value;
	}

	/// <summary>
	/// Target position size for the first futures contract. Positive for long, negative for short.
	/// </summary>
	public int Lot1
	{
		get => _lot1.Value;
		set => _lot1.Value = value;
	}

	/// <summary>
	/// Target position size for the second futures contract. Positive for long, negative for short.
	/// </summary>
	public int Lot2
	{
		get => _lot2.Value;
		set => _lot2.Value = value;
	}

	/// <summary>
	/// Target position size for the third futures contract. Positive for long, negative for short.
	/// </summary>
	public int Lot3
	{
		get => _lot3.Value;
		set => _lot3.Value = value;
	}

	/// <summary>
	/// Hours before contract expiration when the strategy starts the roll process.
	/// </summary>
	public int HoursBeforeExpiration
	{
		get => _hoursBeforeExpiration.Value;
		set => _hoursBeforeExpiration.Value = value;
	}

	/// <summary>
	/// Candle type used as a heartbeat to re-evaluate contracts.
	/// </summary>
	public DataType MonitoringCandleType
	{
		get => _monitoringCandleType.Value;
		set => _monitoringCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FuturesPortfolioControlExpirationStrategy"/> class.
	/// </summary>
	public FuturesPortfolioControlExpirationStrategy()
	{
		_boardCode = Param(nameof(BoardCode), "FORTS")
			.SetDisplay("Board Code", "Exchange board code appended to futures identifiers", "General");

		_symbol1 = Param(nameof(Symbol1), "MXI")
			.SetDisplay("Symbol 1", "Short code of the first futures family", "Portfolio");

		_symbol2 = Param(nameof(Symbol2), "BR")
			.SetDisplay("Symbol 2", "Short code of the second futures family", "Portfolio");

		_symbol3 = Param(nameof(Symbol3), "SBRF")
			.SetDisplay("Symbol 3", "Short code of the third futures family", "Portfolio");

		_lot1 = Param(nameof(Lot1), -4)
			.SetDisplay("Lot 1", "Target position size for the first futures contract", "Portfolio");

		_lot2 = Param(nameof(Lot2), -1)
			.SetDisplay("Lot 2", "Target position size for the second futures contract", "Portfolio");

		_lot3 = Param(nameof(Lot3), 5)
			.SetDisplay("Lot 3", "Target position size for the third futures contract", "Portfolio");

		_hoursBeforeExpiration = Param(nameof(HoursBeforeExpiration), 25)
			.SetGreaterOrEqualZero()
			.SetDisplay("Hours Before Expiration", "When to roll before contract expiration", "Roll Settings");

		_monitoringCandleType = Param(nameof(MonitoringCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Monitoring Candle", "Candle type used as a timer for checks", "General");

		_legs = new[]
		{
			new LegState("Leg 1", () => Symbol1, () => Lot1),
			new LegState("Leg 2", () => Symbol2, () => Lot2),
			new LegState("Leg 3", () => Symbol3, () => Lot3)
		};
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var leg in _legs)
		{
			if (leg.CurrentSecurity != null)
				yield return (leg.CurrentSecurity, MonitoringCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var leg in _legs)
			leg.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var leg in _legs)
			InitializeLeg(leg, time);
	}

	private void InitializeLeg(LegState leg, DateTimeOffset currentTime)
	{
		leg.Reset();

		var symbol = leg.GetSymbol();
		if (string.IsNullOrWhiteSpace(symbol))
		{
			LogWarning($"{leg.Name}: symbol is not specified.");
			return;
		}

		var future = FindFuture(symbol, currentTime, null);
		if (future == null)
		{
			LogWarning($"{leg.Name}: no active contract found for '{symbol}'.");
			return;
		}

		leg.CurrentSecurity = future;
		LogInfo($"{leg.Name}: assigned contract {future.Id} expiring {future.ExpiryDate:yyyy-MM-dd HH:mm}.");

		SubscribeLeg(leg, future);
		EnsureLegPosition(leg);
	}

	private void SubscribeLeg(LegState leg, Security security)
	{
		var subscription = SubscribeCandles(MonitoringCandleType, true, security);

		subscription.WhenNew(candle =>
		{
			if (!ReferenceEquals(leg.CurrentSecurity, security))
				return;

			if (candle.State != CandleStates.Finished)
				return;

			ProcessLeg(leg, candle);
		}).Start();
	}

	private void ProcessLeg(LegState leg, ICandleMessage candle)
	{
		var security = leg.CurrentSecurity;
		if (security == null)
			return;

		var checkTime = candle.CloseTime;
		var expiry = security.ExpiryDate;

		if (expiry == null)
		{
			EnsureLegPosition(leg);
			return;
		}

		var threshold = TimeSpan.FromHours(Math.Max(0, HoursBeforeExpiration));
		var timeToExpiry = expiry.Value - checkTime;

		if (timeToExpiry <= threshold)
		{
			if (!leg.IsRolling)
			{
				leg.IsRolling = true;
				LogInfo($"{leg.Name}: rolling {security.Id} expiring {expiry:yyyy-MM-dd HH:mm}.");
				RollToNextContract(leg, checkTime);
			}
		}
		else
		{
			leg.IsRolling = false;
			EnsureLegPosition(leg);
		}
	}

	private void EnsureLegPosition(LegState leg)
	{
		var security = leg.CurrentSecurity;
		if (security == null)
			return;

		var target = (decimal)leg.GetLot();
		var current = GetPositionValue(security, Portfolio) ?? 0m;

		if (current == target)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (current != 0m && Math.Sign(current) != Math.Sign(target) && target != 0m)
		{
			ClosePosition(security);
			current = 0m;
		}

		var difference = target - current;
		if (difference == 0m)
			return;

		var volume = Math.Abs(difference);
		if (difference > 0m)
		{
			BuyMarket(volume, security);
			LogInfo($"{leg.Name}: increased exposure by {volume} on {security.Id} (target {target}).");
		}
		else
		{
			SellMarket(volume, security);
			LogInfo($"{leg.Name}: decreased exposure by {volume} on {security.Id} (target {target}).");
		}
	}

	private void RollToNextContract(LegState leg, DateTimeOffset currentTime)
	{
		var security = leg.CurrentSecurity;
		if (security == null)
			return;

		ClosePosition(security);
		var currentExpiry = security.ExpiryDate;
		var next = FindFuture(leg.GetSymbol(), currentTime, currentExpiry);
		if (next == null)
		{
			LogWarning($"{leg.Name}: next contract not found after {security.Id}.");
			leg.IsRolling = false;
			return;
		}

		leg.CurrentSecurity = next;
		LogInfo($"{leg.Name}: switched to {next.Id} expiring {next.ExpiryDate:yyyy-MM-dd HH:mm}.");
		SubscribeLeg(leg, next);
		leg.IsRolling = false;
		EnsureLegPosition(leg);
	}

	private Security? FindFuture(string shortName, DateTimeOffset referenceTime, DateTimeOffset? minExpiryExclusive)
	{
		if (SecurityProvider == null)
			return null;

		var month = referenceTime.Month;
		var year = referenceTime.Year;

		for (var i = 0; i < 12; i++)
		{
			if (month > 12)
			{
				month = 1;
				year++;
			}

			foreach (var code in BuildCandidateCodes(shortName, month, year))
			{
				var security = LookupSecurity(code);
				if (security == null)
					continue;

				var expiry = security.ExpiryDate;
				if (expiry == null)
					continue;

				if (expiry <= referenceTime)
					continue;

				if (minExpiryExclusive != null && expiry <= minExpiryExclusive.Value)
					continue;

				return security;
			}

			month++;
		}

		return null;
	}

	private IEnumerable<string> BuildCandidateCodes(string shortName, int month, int year)
	{
		var monthStr = month.ToString(CultureInfo.InvariantCulture);
		var monthPadded = month.ToString("00", CultureInfo.InvariantCulture);
		var yearSuffix = (year % 100).ToString("00", CultureInfo.InvariantCulture);

		yield return $"{shortName}-{monthStr}.{yearSuffix}";
		yield return $"{shortName}-{monthPadded}.{yearSuffix}";
		yield return $"{shortName}{monthPadded}{yearSuffix}";
		yield return $"{shortName}{monthStr}{yearSuffix}";
	}

	private Security? LookupSecurity(string code)
	{
		if (SecurityProvider == null)
			return null;

		Security? security = null;
		var board = BoardCode;
		if (!string.IsNullOrWhiteSpace(board))
		{
			security = SecurityProvider.LookupById($"{code}@{board}");
		}

		return security ?? SecurityProvider.LookupById(code);
	}

	private sealed class LegState
	{
		private readonly Func<string> _getSymbol;
		private readonly Func<int> _getLot;

		public LegState(string name, Func<string> getSymbol, Func<int> getLot)
		{
			Name = name;
			_getSymbol = getSymbol;
			_getLot = getLot;
		}

		public string Name { get; }

		public Security? CurrentSecurity { get; set; }

		public bool IsRolling { get; set; }

		public string GetSymbol() => _getSymbol();

		public int GetLot() => _getLot();

		public void Reset()
		{
			CurrentSecurity = null;
			IsRolling = false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fundamental news trading strategy converted from the DailyFX calendar EA.
/// </summary>
public class FundamentalTraderDailyFxStrategy : Strategy
{
	private readonly StrategyParam<string> _calendarFilePath;
	private readonly StrategyParam<int> _waitingMinutes;
	private readonly StrategyParam<bool> _enableCloseByTime;
	private readonly StrategyParam<int> _closeAfterMinutes;
	private readonly StrategyParam<int> _riskPips;
	private readonly StrategyParam<decimal> _rewardMultiplier;
	private readonly StrategyParam<int> _timerFrequencySeconds;
	private readonly StrategyParam<int> _calendarTimeZoneOffsetHours;
	private readonly StrategyParam<string> _currencyMap;

	private readonly StrategyParam<decimal>[] _volumeLevels = new StrategyParam<decimal>[18];

	private readonly Dictionary<string, string> _currencyToSecurityCode = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Security> _currencyToSecurity = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<Security, QuoteInfo> _quotes = new();
	private readonly Dictionary<Security, PositionState> _positions = new();
	private readonly HashSet<string> _executedEvents = new();
	private readonly HashSet<Security> _subscribedSecurities = new();
	private readonly List<(Security sec, DataType dt)> _working = new();

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FundamentalTraderDailyFxStrategy()
	{
		_calendarFilePath = Param(nameof(CalendarFilePath), "Calendar.csv")
		.SetDisplay("Calendar File", "Path to the DailyFX CSV calendar file.", "General");

		_waitingMinutes = Param(nameof(WaitingMinutes), 27)
		.SetGreaterThanZero()
		.SetDisplay("Waiting Minutes", "Minutes window around the event to allow trading.", "General");

		_enableCloseByTime = Param(nameof(EnableCloseByTime), false)
		.SetDisplay("Enable Timed Exit", "Close positions after a fixed time span.", "Risk");

		_closeAfterMinutes = Param(nameof(CloseAfterMinutes), 20)
		.SetGreaterThanZero()
		.SetDisplay("Timed Exit Minutes", "Minutes after entry to close positions when enabled.", "Risk");

		_riskPips = Param(nameof(RiskPips), 20)
		.SetGreaterThanZero()
		.SetDisplay("Risk (pips)", "Stop-loss distance in pips.", "Risk");

		_rewardMultiplier = Param(nameof(RewardMultiplier), 3m)
		.SetDisplay("Reward Multiplier", "Multiplier applied to the risk distance for take-profit.", "Risk");

		_timerFrequencySeconds = Param(nameof(TimerFrequencySeconds), 30)
		.SetGreaterThanZero()
		.SetDisplay("Timer Frequency (sec)", "Frequency of CSV polling and timed exit checks.", "General");

		_calendarTimeZoneOffsetHours = Param(nameof(CalendarTimeZoneOffsetHours), 0)
		.SetDisplay("Calendar TZ Offset (h)", "Offset in hours applied to calendar timestamps.", "General");

		_currencyMap = Param(nameof(CurrencyMap), "EUR=EURUSD;USD=EURUSD;JPY=USDJPY;GBP=GBPUSD;CHF=USDCHF;AUD=AUDUSD;CAD=USDCAD;NZD=NZDUSD")
		.SetDisplay("Currency Map", "Mapping from calendar currency to tradable symbol.", "General");

		var defaultVolumes = new[]
		{
			0.01m, 0.02m, 0.03m, 0.04m, 0.05m, 0.06m, 0.07m, 0.08m, 0.09m,
			0.1m, 0.11m, 0.12m, 0.13m, 0.14m, 0.15m, 0.16m, 0.17m, 0.17m
		};

		var descriptions = new[]
		{
			"0-3%", "3-6%", "6-9%", "9-12%", "12-15%", "15-18%", "18-21%", "21-24%",
			"24-27%", "27-30%", "30-40%", "40-50%", "50-60%", "60-70%", "70-80%", "80-90%", "90-100%", ">100%"
		};

		for (var i = 0; i < _volumeLevels.Length; i++)
		{
			var index = i;
			_volumeLevels[i] = Param($"VolumeLevel{index + 1}", defaultVolumes[i])
			.SetDisplay($"Volume Level {index + 1}", $"Order volume when deviation is within {descriptions[index]}.", "Risk");
		}
	}

	/// <summary>
	/// Path to the CSV calendar file.
	/// </summary>
	public string CalendarFilePath
	{
		get => _calendarFilePath.Value;
		set => _calendarFilePath.Value = value;
	}

	/// <summary>
	/// Minutes around the event time when trades are permitted.
	/// </summary>
	public int WaitingMinutes
	{
		get => _waitingMinutes.Value;
		set => _waitingMinutes.Value = value;
	}

	/// <summary>
	/// Enable automatic closing of positions after a fixed time.
	/// </summary>
	public bool EnableCloseByTime
	{
		get => _enableCloseByTime.Value;
		set => _enableCloseByTime.Value = value;
	}

	/// <summary>
	/// Minutes to hold the trade before the timed exit triggers.
	/// </summary>
	public int CloseAfterMinutes
	{
		get => _closeAfterMinutes.Value;
		set => _closeAfterMinutes.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int RiskPips
	{
		get => _riskPips.Value;
		set => _riskPips.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier relative to the risk distance.
	/// </summary>
	public decimal RewardMultiplier
	{
		get => _rewardMultiplier.Value;
		set => _rewardMultiplier.Value = value;
	}

	/// <summary>
	/// Timer frequency for CSV polling and timed exit checks.
	/// </summary>
	public int TimerFrequencySeconds
	{
		get => _timerFrequencySeconds.Value;
		set => _timerFrequencySeconds.Value = value;
	}

	/// <summary>
	/// Offset applied to calendar timestamps.
	/// </summary>
	public int CalendarTimeZoneOffsetHours
	{
		get => _calendarTimeZoneOffsetHours.Value;
		set => _calendarTimeZoneOffsetHours.Value = value;
	}

	/// <summary>
	/// Mapping from calendar currency codes to tradable instruments.
	/// </summary>
	public string CurrencyMap
	{
		get => _currencyMap.Value;
		set => _currencyMap.Value = value;
	}

	/// <summary>
	/// Volume level for 0-3% deviation.
	/// </summary>
	public decimal VolumeLevel1
	{
		get => _volumeLevels[0].Value;
		set => _volumeLevels[0].Value = value;
	}

	/// <summary>
	/// Volume level for 3-6% deviation.
	/// </summary>
	public decimal VolumeLevel2
	{
		get => _volumeLevels[1].Value;
		set => _volumeLevels[1].Value = value;
	}

	/// <summary>
	/// Volume level for 6-9% deviation.
	/// </summary>
	public decimal VolumeLevel3
	{
		get => _volumeLevels[2].Value;
		set => _volumeLevels[2].Value = value;
	}

	/// <summary>
	/// Volume level for 9-12% deviation.
	/// </summary>
	public decimal VolumeLevel4
	{
		get => _volumeLevels[3].Value;
		set => _volumeLevels[3].Value = value;
	}

	/// <summary>
	/// Volume level for 12-15% deviation.
	/// </summary>
	public decimal VolumeLevel5
	{
		get => _volumeLevels[4].Value;
		set => _volumeLevels[4].Value = value;
	}

	/// <summary>
	/// Volume level for 15-18% deviation.
	/// </summary>
	public decimal VolumeLevel6
	{
		get => _volumeLevels[5].Value;
		set => _volumeLevels[5].Value = value;
	}

	/// <summary>
	/// Volume level for 18-21% deviation.
	/// </summary>
	public decimal VolumeLevel7
	{
		get => _volumeLevels[6].Value;
		set => _volumeLevels[6].Value = value;
	}

	/// <summary>
	/// Volume level for 21-24% deviation.
	/// </summary>
	public decimal VolumeLevel8
	{
		get => _volumeLevels[7].Value;
		set => _volumeLevels[7].Value = value;
	}

	/// <summary>
	/// Volume level for 24-27% deviation.
	/// </summary>
	public decimal VolumeLevel9
	{
		get => _volumeLevels[8].Value;
		set => _volumeLevels[8].Value = value;
	}

	/// <summary>
	/// Volume level for 27-30% deviation.
	/// </summary>
	public decimal VolumeLevel10
	{
		get => _volumeLevels[9].Value;
		set => _volumeLevels[9].Value = value;
	}

	/// <summary>
	/// Volume level for 30-40% deviation.
	/// </summary>
	public decimal VolumeLevel11
	{
		get => _volumeLevels[10].Value;
		set => _volumeLevels[10].Value = value;
	}

	/// <summary>
	/// Volume level for 40-50% deviation.
	/// </summary>
	public decimal VolumeLevel12
	{
		get => _volumeLevels[11].Value;
		set => _volumeLevels[11].Value = value;
	}

	/// <summary>
	/// Volume level for 50-60% deviation.
	/// </summary>
	public decimal VolumeLevel13
	{
		get => _volumeLevels[12].Value;
		set => _volumeLevels[12].Value = value;
	}

	/// <summary>
	/// Volume level for 60-70% deviation.
	/// </summary>
	public decimal VolumeLevel14
	{
		get => _volumeLevels[13].Value;
		set => _volumeLevels[13].Value = value;
	}

	/// <summary>
	/// Volume level for 70-80% deviation.
	/// </summary>
	public decimal VolumeLevel15
	{
		get => _volumeLevels[14].Value;
		set => _volumeLevels[14].Value = value;
	}

	/// <summary>
	/// Volume level for 80-90% deviation.
	/// </summary>
	public decimal VolumeLevel16
	{
		get => _volumeLevels[15].Value;
		set => _volumeLevels[15].Value = value;
	}

	/// <summary>
	/// Volume level for 90-100% deviation.
	/// </summary>
	public decimal VolumeLevel17
	{
		get => _volumeLevels[16].Value;
		set => _volumeLevels[16].Value = value;
	}

	/// <summary>
	/// Volume level for deviations above 100%.
	/// </summary>
	public decimal VolumeLevel18
	{
		get => _volumeLevels[17].Value;
		set => _volumeLevels[17].Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var item in _working)
		yield return item;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currencyToSecurityCode.Clear();
		_currencyToSecurity.Clear();
		_quotes.Clear();
		_positions.Clear();
		_executedEvents.Clear();
		_subscribedSecurities.Clear();
		_working.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		TimerInterval = TimeSpan.FromSeconds(TimerFrequencySeconds);

		RebuildCurrencyMapping();
		ProcessCalendar();
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		ProcessCalendar();

		if (EnableCloseByTime)
		CheckTimedExits();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var security = trade.Order?.Security;
		if (security == null)
		return;

		if (!_positions.TryGetValue(security, out var state))
		return;

		var position = GetPositionValue(security);
		// Close the tracking state once the position is fully exited elsewhere.

		if (position == 0m)
		{
			_positions.Remove(security);
			return;
		}

		state.Volume = Math.Abs(position);

		var sign = Math.Sign(position);
		if (sign > 0)
		state.Side = Sides.Buy;
		else if (sign < 0)
		state.Side = Sides.Sell;

		state.IsActive = true;
		state.ExitPending = false;
		state.OpenTime = trade.Trade.Time != default ? trade.Trade.Time : CurrentTime;
	}

	private void RebuildCurrencyMapping()
	{
		// Refresh configured currency mapping and ensure price subscriptions.
		ParseCurrencyMap();

		_currencyToSecurity.Clear();
		_working.Clear();

		foreach (var pair in _currencyToSecurityCode)
		{
			var security = LookupSecurity(pair.Value);
			if (security == null)
			{
				LogWarning($"Security '{pair.Value}' for currency '{pair.Key}' was not found.");
				continue;
			}

			_currencyToSecurity[pair.Key] = security;

			if (_subscribedSecurities.Add(security))
			{
				SubscribeLevel1(security)
				.Bind(message => ProcessLevel1(security, message))
				.Start();
			}

			_working.Add((security, DataType.Level1));
		}
	}

	private void ProcessCalendar()
	{
		var events = ReadCalendarEvents();
		// Evaluate every calendar entry within the active trading window.
		if (events.Count == 0)
		return;

		var nowUtc = CurrentTime.UtcDateTime;

		foreach (var calendarEvent in events)
		{
			if (!IsEventEligible(calendarEvent, nowUtc))
			continue;

			if (calendarEvent.Actual.HasValue && calendarEvent.Forecast.HasValue && calendarEvent.Forecast.Value != 0m)
			{
				var deviation = Math.Abs((calendarEvent.Actual.Value - calendarEvent.Forecast.Value) / calendarEvent.Forecast.Value * 100m);
				var isPositive = calendarEvent.Actual.Value > calendarEvent.Forecast.Value;

				var eventKey = $"{calendarEvent.Currency}|{calendarEvent.EventTime:O}|F|{calendarEvent.RawActual}|{calendarEvent.RawForecast}|{(isPositive ? "UP" : "DOWN")}";
				if (_executedEvents.Contains(eventKey))
				continue;

				if (TryOpenPosition(calendarEvent, isPositive, deviation, eventKey))
				continue;
			}
			else if (calendarEvent.Actual.HasValue && calendarEvent.Previous.HasValue && calendarEvent.Previous.Value != 0m)
			{
				var deviation = Math.Abs((calendarEvent.Actual.Value - calendarEvent.Previous.Value) / calendarEvent.Previous.Value * 100m);
				var isPositive = calendarEvent.Actual.Value > calendarEvent.Previous.Value;

				var eventKey = $"{calendarEvent.Currency}|{calendarEvent.EventTime:O}|P|{calendarEvent.RawActual}|{calendarEvent.RawPrevious}|{(isPositive ? "UP" : "DOWN")}";
				if (_executedEvents.Contains(eventKey))
				continue;

				TryOpenPosition(calendarEvent, isPositive, deviation, eventKey);
			}
		}
	}

	private bool TryOpenPosition(CalendarEvent calendarEvent, bool isPositive, decimal deviation, string eventKey)
	{
		var currency = calendarEvent.Currency.Trim();
		// Skip malformed entries without a currency code.
		if (string.IsNullOrWhiteSpace(currency))
		return false;

		var security = ResolveSecurity(currency);
		// Map the calendar currency to an instrument configured for trading.
		if (security == null)
		{
			LogWarning($"No security configured for currency '{currency}'.");
			return false;
		}

		_currencyToSecurityCode.TryGetValue(currency, out var securityCode);
		securityCode ??= security.Code;

		var side = DetermineSide(currency, securityCode, isPositive);

		var position = GetPositionValue(security);
		// Do not open a new position if one with the same direction already exists.
		if (position > 0m && side == Sides.Buy)
		{
			LogInfo($"Skip {currency} event because a long position already exists on {security.Code}.");
			return false;
		}

		if (position < 0m && side == Sides.Sell)
		{
			LogInfo($"Skip {currency} event because a short position already exists on {security.Code}.");
			return false;
		}

		if (position != 0m)
		{
			LogInfo($"Skip {currency} event because an opposite position ({position:F2}) is still open on {security.Code}.");
			return false;
		}

		var volume = GetVolumeForDeviation(deviation);
		if (volume <= 0m)
		{
			LogInfo($"Skip {currency} event because the calculated volume is zero (deviation {deviation:F2}%).");
			return false;
		}

		var adjustedVolume = AdjustVolume(volume, security);
		if (adjustedVolume <= 0m)
		{
			LogWarning($"Unable to calculate a valid volume for {security.Code}.");
			return false;
		}

		if (!_quotes.TryGetValue(security, out var quote))
		{
			quote = new QuoteInfo();
			// Initialize quote storage for the instrument.
			_quotes[security] = quote;
		}

		var entryPrice = quote.GetPrice(side);
		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		decimal? stopPrice = null;
		// Convert the pip-based stop and take levels into actual price values when possible.
		decimal? takePrice = null;

		if (step > 0m && RiskPips > 0)
		{
			var riskDistance = step * RiskPips;
			var takeDistance = RewardMultiplier > 0m ? riskDistance * RewardMultiplier : 0m;

			if (entryPrice.HasValue)
			{
				if (side == Sides.Buy)
				{
					stopPrice = entryPrice.Value - riskDistance;
					if (takeDistance > 0m)
					takePrice = entryPrice.Value + takeDistance;
				}
				else
				{
					stopPrice = entryPrice.Value + riskDistance;
					if (takeDistance > 0m)
					takePrice = entryPrice.Value - takeDistance;
				}
			}
		}

		try
		{
			if (side == Sides.Buy)
			{
			// Positive surprise -> buy the base currency.
			BuyMarket(adjustedVolume, security);
			}
			else
			{
			// Negative surprise -> sell the base currency.
			SellMarket(adjustedVolume, security);
			}

			LogInfo($"News trade for {currency} on {security.Code}: {side}, volume {adjustedVolume:F2}, deviation {deviation:F2}%.");

			// Remember protective levels to manage the position as new quotes arrive.
			_positions[security] = new PositionState
			{
				Currency = currency,
				Side = side,
				Volume = adjustedVolume,
				OpenTime = CurrentTime,
				StopPrice = stopPrice,
				TakePrice = takePrice,
				IsActive = false,
				ExitPending = false
			};

			_executedEvents.Add(eventKey);
			return true;
		}
		catch (Exception ex)
		{
			LogError($"Failed to send order for {security.Code}: {ex.Message}");
			return false;
		}
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		if (!_quotes.TryGetValue(security, out var quote))
		{
			quote = new QuoteInfo();
			// Initialize quote storage for the instrument.
			_quotes[security] = quote;
		}

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid.HasValue && bid.Value > 0m)
		quote.Bid = bid.Value;

		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask.HasValue && ask.Value > 0m)
		quote.Ask = ask.Value;

		ManagePosition(security, quote);
	}

	private void ManagePosition(Security security, QuoteInfo quote)
	{
		if (!_positions.TryGetValue(security, out var state))
		return;

		var position = GetPositionValue(security);
		// Close the tracking state once the position is fully exited elsewhere.
		if (position == 0m)
		{
			_positions.Remove(security);
			return;
		}

		if (!state.IsActive)
		{
			var sign = Math.Sign(position);
			if ((state.Side == Sides.Buy && sign > 0) || (state.Side == Sides.Sell && sign < 0))
			{
				state.IsActive = true;
				state.OpenTime = CurrentTime;
			}
		}

		if (state.ExitPending)
		return;

		if (state.Side == Sides.Buy)
		{
			// For long trades the bid price must reach the stop or take-profit levels.
			if (state.StopPrice.HasValue && quote.Bid.HasValue && quote.Bid.Value <= state.StopPrice.Value)
			{
				state.ExitPending = true;
				SellMarket(Math.Abs(position), security);
				LogInfo($"Stop-loss triggered for {security.Code} at {quote.Bid.Value:F5}.");
				return;
			}

			if (state.TakePrice.HasValue && quote.Bid.HasValue && quote.Bid.Value >= state.TakePrice.Value)
			{
				state.ExitPending = true;
				SellMarket(Math.Abs(position), security);
				LogInfo($"Take-profit triggered for {security.Code} at {quote.Bid.Value:F5}.");
			}
		}
		else
		{
			// For short trades watch the ask side for protective triggers.
			if (state.StopPrice.HasValue && quote.Ask.HasValue && quote.Ask.Value >= state.StopPrice.Value)
			{
				state.ExitPending = true;
				BuyMarket(Math.Abs(position), security);
				LogInfo($"Stop-loss triggered for {security.Code} at {quote.Ask.Value:F5}.");
				return;
			}

			if (state.TakePrice.HasValue && quote.Ask.HasValue && quote.Ask.Value <= state.TakePrice.Value)
			{
				state.ExitPending = true;
				BuyMarket(Math.Abs(position), security);
				LogInfo($"Take-profit triggered for {security.Code} at {quote.Ask.Value:F5}.");
			}
		}
	}

	private void CheckTimedExits()
	{
		if (_positions.Count == 0)
		return;

		var now = CurrentTime;
		// Collect instruments that exceeded the permitted holding time.
		var toClose = new List<Security>();

		foreach (var pair in _positions)
		{
			var state = pair.Value;
			if (!state.IsActive || state.ExitPending)
			continue;

			var elapsed = now - state.OpenTime;
			if (elapsed.TotalMinutes >= CloseAfterMinutes)
			toClose.Add(pair.Key);
		}

		foreach (var security in toClose)
		{
			if (!_positions.TryGetValue(security, out var state))
			continue;

			var position = GetPositionValue(security);
			if (position == 0m)
			{
				_positions.Remove(security);
				continue;
			}

			LogInfo($"Timed exit triggered for {security.Code} after {CloseAfterMinutes} minutes.");
			state.ExitPending = true;
			ClosePosition(security);
		}
	}

	private void ParseCurrencyMap()
	{
		_currencyToSecurityCode.Clear();

		var map = CurrencyMap;
		// Parse user supplied mapping string like "EUR=EURUSD;USD=EURUSD".
		if (string.IsNullOrWhiteSpace(map))
		return;

		var separators = new[] { ';', '\n', '\r' };
		var entries = map.Split(separators, StringSplitOptions.RemoveEmptyEntries);

		foreach (var entry in entries)
		{
			var parts = entry.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2)
			continue;

			var currency = parts[0].Trim();
			var code = parts[1].Trim();

			if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(code))
			continue;

			_currencyToSecurityCode[currency.ToUpperInvariant()] = code;
		}
	}

	private Security? ResolveSecurity(string currency)
	{
		var key = currency.ToUpperInvariant();
		if (_currencyToSecurity.TryGetValue(key, out var security))
		return security;

		if (_currencyToSecurityCode.TryGetValue(key, out var code))
		{
			security = LookupSecurity(code);
			if (security != null)
			{
				_currencyToSecurity[key] = security;

				if (_subscribedSecurities.Add(security))
				{
					SubscribeLevel1(security)
					.Bind(message => ProcessLevel1(security, message))
					.Start();
				}

				if (!_working.Contains((security, DataType.Level1)))
				_working.Add((security, DataType.Level1));
			}
		}

		return security;
	}

	private Security? LookupSecurity(string code)
	{
		if (SecurityProvider == null)
		return null;

		Security? security = null;
		var board = BoardCode;
		if (!string.IsNullOrWhiteSpace(board))
		security = SecurityProvider.LookupById($"{code}@{board}");

		return security ?? SecurityProvider.LookupById(code);
	}

	private static decimal AdjustVolume(decimal volume, Security security)
	{
		var min = security.VolumeMin ?? 0m;
		var step = security.VolumeStep ?? 0m;

		if (step <= 0m && min > 0m)
		step = min;

		if (step <= 0m)
		step = 1m;

		var adjusted = volume;

		if (min > 0m && adjusted < min)
		adjusted = min;

		if (step > 0m)
		{
			adjusted = Math.Round(adjusted / step, MidpointRounding.AwayFromZero) * step;
		}

		if (min > 0m && adjusted < min)
		adjusted = min;

		return adjusted;
	}

	private decimal GetVolumeForDeviation(decimal deviation)
	{
		var levels = new[]
		{
			VolumeLevel1, VolumeLevel2, VolumeLevel3, VolumeLevel4, VolumeLevel5, VolumeLevel6,
			VolumeLevel7, VolumeLevel8, VolumeLevel9, VolumeLevel10, VolumeLevel11, VolumeLevel12,
			VolumeLevel13, VolumeLevel14, VolumeLevel15, VolumeLevel16, VolumeLevel17, VolumeLevel18
		};

		var thresholds = new[]
		{
			3m, 6m, 9m, 12m, 15m, 18m, 21m, 24m, 27m, 30m, 40m, 50m, 60m, 70m, 80m, 90m, 100m
		};

		for (var i = 0; i < thresholds.Length; i++)
		{
			if (deviation <= thresholds[i])
			return levels[i];
		}

		return levels[^1];
	}

	private static Sides DetermineSide(string currency, string securityCode, bool isPositive)
	{
		var upperCurrency = currency.ToUpperInvariant();
		var upperCode = securityCode.ToUpperInvariant();

		if (upperCode.StartsWith(upperCurrency, StringComparison.Ordinal))
		return isPositive ? Sides.Buy : Sides.Sell;

		if (upperCode.EndsWith(upperCurrency, StringComparison.Ordinal))
		return isPositive ? Sides.Sell : Sides.Buy;

		return upperCurrency is "CHF" or "CAD" or "JPY" or "USD"
		? (isPositive ? Sides.Sell : Sides.Buy)
		: (isPositive ? Sides.Buy : Sides.Sell);
	}

	private bool IsEventEligible(CalendarEvent calendarEvent, DateTime nowUtc)
	{
		if (!string.Equals(calendarEvent.Importance, "High", StringComparison.OrdinalIgnoreCase))
		return false;

		var eventTimeUtc = calendarEvent.EventTime.UtcDateTime;
		var minutes = (nowUtc - eventTimeUtc).TotalMinutes;

		if (minutes < 0)
		{
			if (-minutes > WaitingMinutes)
			return false;

			if (!calendarEvent.Actual.HasValue)
			return false;
		}
		else if (minutes > WaitingMinutes)
		{
			return false;
		}

		return true;
	}

	private List<CalendarEvent> ReadCalendarEvents()
	{
		var result = new List<CalendarEvent>();
		var path = CalendarFilePath;

		if (string.IsNullOrWhiteSpace(path))
		return result;

		try
		{
			if (!File.Exists(path))
			{
				LogWarning($"Calendar file '{path}' not found.");
				return result;
			}

			using var reader = new StreamReader(path);
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(line))
				continue;

				var cells = SplitCsvLine(line);
				if (cells.Count < 9)
				continue;

				var date = cells[0].Trim();
				if (date.Equals("Date", StringComparison.OrdinalIgnoreCase))
				continue;

				var time = cells[1].Trim();
				var timeZone = cells[2].Trim();
				var currency = cells[3].Trim();
				var description = cells[4].Trim();
				var importance = cells[5].Trim();
				var actual = cells[6].Trim();
				var forecast = cells[7].Trim();
				var previous = cells[8].Trim();

				var eventTime = ParseEventTime(date, time, timeZone);
				if (eventTime == null)
				continue;

				result.Add(new CalendarEvent
				{
					EventTime = eventTime.Value,
					Currency = currency,
					Importance = importance,
					Description = description,
					RawActual = actual,
					RawForecast = forecast,
					RawPrevious = previous,
					Actual = ParseNullableDecimal(actual),
					Forecast = ParseNullableDecimal(forecast),
					Previous = ParseNullableDecimal(previous)
				});
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to read calendar file '{path}': {ex.Message}");
		}

		return result;
	}

	private DateTimeOffset? ParseEventTime(string dateText, string timeText, string timeZoneText)
	{
		if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(timeText))
		return null;

		var timeLabel = timeText.Trim();
		if (timeLabel.Equals("Time", StringComparison.OrdinalIgnoreCase) ||
		timeLabel.Equals("All Day", StringComparison.OrdinalIgnoreCase) ||
		timeLabel.Equals("Tentative", StringComparison.OrdinalIgnoreCase))
		return null;

		var combined = $"{dateText} {timeLabel}";
		if (!DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
		{
			if (!DateTime.TryParse(combined, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out parsed))
			return null;
		}

		var offset = TimeSpan.FromHours(CalendarTimeZoneOffsetHours);
		var eventTime = new DateTimeOffset(parsed, offset);
		return eventTime.ToUniversalTime();
	}

	private static decimal? ParseNullableDecimal(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		return null;

		var text = value.Trim();
		if (text.Equals("n/a", StringComparison.OrdinalIgnoreCase) || text.Equals("na", StringComparison.OrdinalIgnoreCase))
		return null;

		var negative = false;
		if (text.StartsWith('(') && text.EndsWith(')'))
		{
			negative = true;
			text = text[1..^1];
		}

		var builder = new StringBuilder(text.Length);
		foreach (var ch in text)
		{
			if (char.IsDigit(ch) || ch == '.' || ch == '-' || ch == ',')
			builder.Append(ch);
		}

		var sanitized = builder.ToString().Replace(",", "");
		if (sanitized.Length == 0)
		return null;

		if (!decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		return null;

		return negative ? -result : result;
	}

	private static List<string> SplitCsvLine(string line)
	{
		var result = new List<string>();
		var builder = new StringBuilder();
		// Minimal CSV parser that understands quoted cells.
		var inQuotes = false;

		for (var i = 0; i < line.Length; i++)
		{
			var ch = line[i];
			if (ch == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					builder.Append('"');
					i += 1;
				}
				else
				{
					inQuotes = !inQuotes;
				}

				continue;
			}

			if (ch == ',' && !inQuotes)
			{
				result.Add(builder.ToString());
				builder.Clear();
				continue;
			}

			builder.Append(ch);
		}

		result.Add(builder.ToString());
		return result;
	}

	private sealed class PositionState
	{
		public string Currency { get; set; } = string.Empty;
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public DateTimeOffset OpenTime { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? TakePrice { get; set; }
		public bool IsActive { get; set; }
		public bool ExitPending { get; set; }
	}

	private sealed class QuoteInfo
	{
		public decimal? Bid { get; set; }
		public decimal? Ask { get; set; }

		public decimal? GetPrice(Sides side)
		{
			return side == Sides.Buy ? Ask ?? GetMid() : Bid ?? GetMid();
		}

		private decimal? GetMid()
		{
			if (Bid.HasValue && Ask.HasValue)
			return (Bid.Value + Ask.Value) / 2m;

			return Bid ?? Ask;
		}
	}

	private sealed class CalendarEvent
	{
		public DateTimeOffset EventTime { get; set; }
		public string Currency { get; set; } = string.Empty;
		public string Importance { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string RawActual { get; set; } = string.Empty;
		public string RawForecast { get; set; } = string.Empty;
		public string RawPrevious { get; set; } = string.Empty;
		public decimal? Actual { get; set; }
		public decimal? Forecast { get; set; }
		public decimal? Previous { get; set; }
	}
}

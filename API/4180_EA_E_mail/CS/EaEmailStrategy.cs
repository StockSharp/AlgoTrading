using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the EA_E-mail MetaTrader expert by sending periodic account summaries.
/// </summary>
public class EaEmailStrategy : Strategy
{
	private readonly StrategyParam<int> _timeIntervalMinutes;

	private readonly HashSet<Order> _activeOrders = new();

	/// <summary>
	/// Interval between account summary e-mails in minutes.
	/// </summary>
	public int TimeIntervalMinutes
	{
		get => _timeIntervalMinutes.Value;
		set => _timeIntervalMinutes.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public EaEmailStrategy()
	{
		_timeIntervalMinutes = Param(nameof(TimeIntervalMinutes), 30)
			.SetGreaterThanZero()
			.SetDisplay("Report Interval (minutes)", "Delay between account summary messages", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SendAccountReport();

		if (TimeIntervalMinutes <= 0)
		{
			LogWarning("Report interval must be greater than zero. Periodic reports are disabled.");
			return;
		}

		if (Security == null)
		{
			LogWarning("Security is not assigned. Only the initial report will be produced.");
			return;
		}

		var candleType = TimeSpan.FromMinutes(TimeIntervalMinutes).TimeFrame();

		SubscribeCandles(candleType)
			.Bind(OnTimerCandle)
			.Start();
	}

	private void OnTimerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		SendAccountReport();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active)
			_activeOrders.Add(order);
		else
			_activeOrders.Remove(order);
	}

	private void SendAccountReport()
	{
		var accountName = Portfolio?.Name;
		if (string.IsNullOrWhiteSpace(accountName))
			accountName = "Unknown";

		var subject = $"Forex Account : {accountName} Details";
		var body = BuildReportBody();

		// MetaTrader used SendMail; here we mimic it with informational log entries.
		LogInfo($"[Email] Subject: {subject}");
		LogInfo($"[Email] Body:{Environment.NewLine}{body}");
	}

	private string BuildReportBody()
	{
		var now = CurrentTime;
		if (now == default)
			now = DateTimeOffset.UtcNow;

		var portfolio = Portfolio;

		var balance = portfolio?.BeginValue ?? portfolio?.CurrentValue ?? 0m;
		var equity = portfolio?.CurrentValue ?? 0m;

		var usedMargin = TryGetPortfolioMetric(portfolio, "BlockedValue") ?? TryGetPortfolioMetric(portfolio, "Margin");
		var freeMargin = usedMargin.HasValue ? balance - usedMargin.Value : (decimal?)null;
		var leverageText = GetPortfolioLeverage(portfolio);

		var builder = new StringBuilder();

		builder.AppendLine($"Date and Time : {now:yyyy-MM-ddTHH:mm:ssK}");
		builder.AppendLine($"Balance       : {FormatDecimal(balance)}");
		builder.AppendLine($"Used Margin   : {FormatOptionalDecimal(usedMargin)}");
		builder.AppendLine($"Free Margin   : {FormatOptionalDecimal(freeMargin)}");
		builder.AppendLine($"Equity        : {FormatDecimal(equity)}");
		builder.AppendLine($"Open Orders   : {CountActiveOrders()}");
		builder.AppendLine();
		builder.AppendLine($"Broker  : {GetBrokerDescription()}");
		builder.Append($"Leverage: {leverageText}");

		return builder.ToString();
	}

	private string GetBrokerDescription()
	{
		var connector = Connector;
		if (connector == null)
			return "Unknown";

		var type = connector.GetType();

		var nameProperty = type.GetProperty("Name");
		if (nameProperty?.GetValue(connector) is string name && !string.IsNullOrWhiteSpace(name))
			return name;

		var idProperty = type.GetProperty("Id");
		if (idProperty?.GetValue(connector) is string id && !string.IsNullOrWhiteSpace(id))
			return id;

		return type.Name;
	}

	private string GetPortfolioLeverage(Portfolio portfolio)
	{
		var leverage = TryGetPortfolioMetric(portfolio, "Leverage");
		return leverage.HasValue ? FormatDecimal(leverage.Value) : "N/A";
	}

	private static decimal? TryGetPortfolioMetric(Portfolio portfolio, string propertyName)
	{
		if (portfolio == null)
			return null;

		var property = portfolio.GetType().GetProperty(propertyName);
		if (property == null)
			return null;

		var value = property.GetValue(portfolio);

		return value switch
		{
			decimal decimalValue => decimalValue,
			double doubleValue => (decimal)doubleValue,
			float floatValue => (decimal)floatValue,
			int intValue => intValue,
			long longValue => longValue,
			_ => null,
		};
	}

	private static string FormatDecimal(decimal value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture);
	}

	private static string FormatOptionalDecimal(decimal? value)
	{
		return value.HasValue ? FormatDecimal(value.Value) : "N/A";
	}

	private int CountActiveOrders()
	{
		return _activeOrders.Count;
	}
}

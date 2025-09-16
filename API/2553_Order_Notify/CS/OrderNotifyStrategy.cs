using System;
using System.Net;
using System.Net.Mail;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that watches own trades and sends notifications when orders are opened or closed.
/// </summary>
public class OrderNotifyStrategy : Strategy
{
	private readonly StrategyParam<bool> _sendEmails;
	private readonly StrategyParam<string> _smtpHost;
	private readonly StrategyParam<int> _smtpPort;
	private readonly StrategyParam<bool> _smtpUseSsl;
	private readonly StrategyParam<string> _smtpUser;
	private readonly StrategyParam<string> _smtpPassword;
	private readonly StrategyParam<string> _emailFrom;
	private readonly StrategyParam<string> _emailTo;
	private readonly StrategyParam<string> _subjectPrefix;

	private decimal _currentPosition;
	private decimal _averagePrice;

	/// <summary>
	/// Initializes a new instance of <see cref="OrderNotifyStrategy"/>.
	/// </summary>
	public OrderNotifyStrategy()
	{
		_sendEmails = Param(nameof(SendEmails), false)
			.SetDisplay("Send Emails", "Enable SMTP email notifications", "Notifications")
			.SetCanOptimize(false);

		_smtpHost = Param(nameof(SmtpHost), string.Empty)
			.SetDisplay("SMTP Host", "Hostname of the SMTP server", "Notifications")
			.SetCanOptimize(false);

		_smtpPort = Param(nameof(SmtpPort), 25)
			.SetDisplay("SMTP Port", "Port of the SMTP server", "Notifications")
			.SetCanOptimize(false);

		_smtpUseSsl = Param(nameof(SmtpUseSsl), true)
			.SetDisplay("Use SSL", "Use SSL/TLS for SMTP connection", "Notifications")
			.SetCanOptimize(false);

		_smtpUser = Param(nameof(SmtpUser), string.Empty)
			.SetDisplay("SMTP User", "Login for SMTP server", "Notifications")
			.SetCanOptimize(false);

		_smtpPassword = Param(nameof(SmtpPassword), string.Empty)
			.SetDisplay("SMTP Password", "Password for SMTP server", "Notifications")
			.SetCanOptimize(false);

		_emailFrom = Param(nameof(EmailFrom), string.Empty)
			.SetDisplay("Email From", "Sender email address", "Notifications")
			.SetCanOptimize(false);

		_emailTo = Param(nameof(EmailTo), string.Empty)
			.SetDisplay("Email To", "Recipient email address", "Notifications")
			.SetCanOptimize(false);

		_subjectPrefix = Param(nameof(SubjectPrefix), "Order Notify: ")
			.SetDisplay("Subject Prefix", "Prefix appended to notification subjects", "Notifications")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Enable or disable SMTP email notifications.
	/// </summary>
	public bool SendEmails
	{
		get => _sendEmails.Value;
		set => _sendEmails.Value = value;
	}

	/// <summary>
	/// Hostname of the SMTP server used for email notifications.
	/// </summary>
	public string SmtpHost
	{
		get => _smtpHost.Value;
		set => _smtpHost.Value = value;
	}

	/// <summary>
	/// Port number of the SMTP server.
	/// </summary>
	public int SmtpPort
	{
		get => _smtpPort.Value;
		set => _smtpPort.Value = value;
	}

	/// <summary>
	/// Indicates whether SSL/TLS is required for SMTP.
	/// </summary>
	public bool SmtpUseSsl
	{
		get => _smtpUseSsl.Value;
		set => _smtpUseSsl.Value = value;
	}

	/// <summary>
	/// Login for SMTP authentication if required.
	/// </summary>
	public string SmtpUser
	{
		get => _smtpUser.Value;
		set => _smtpUser.Value = value;
	}

	/// <summary>
	/// Password for SMTP authentication if required.
	/// </summary>
	public string SmtpPassword
	{
		get => _smtpPassword.Value;
		set => _smtpPassword.Value = value;
	}

	/// <summary>
	/// Sender email address.
	/// </summary>
	public string EmailFrom
	{
		get => _emailFrom.Value;
		set => _emailFrom.Value = value;
	}

	/// <summary>
	/// Recipient email address.
	/// </summary>
	public string EmailTo
	{
		get => _emailTo.Value;
		set => _emailTo.Value = value;
	}

	/// <summary>
	/// Prefix appended to notification subjects.
	/// </summary>
	public string SubjectPrefix
	{
		get => _subjectPrefix.Value;
		set => _subjectPrefix.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentPosition = Position;
		_averagePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade?.Trade == null || trade.Order == null)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0)
			return;

		var price = trade.Trade.Price;
		var direction = trade.Order.Side == Sides.Buy ? 1m : -1m;
		var previousPosition = _currentPosition;
		var previousAveragePrice = _averagePrice;
		var signedVolume = direction * volume;

		// Update current position before calculating reversals to keep notifications consistent with state changes.
		_currentPosition += signedVolume;

		if (previousPosition == 0m || Math.Sign(previousPosition) == Math.Sign(direction))
		{
			// Either opening a brand-new position or adding to an existing one.
			var previousAbs = Math.Abs(previousPosition);
			var newAbs = previousAbs + volume;
			_averagePrice = newAbs > 0m
				? ((previousAbs * previousAveragePrice) + (volume * price)) / newAbs
				: 0m;

			NotifyOrderOpened(trade, volume, price);
			return;
		}

		// Trade direction differs from the existing position, so at least part of the position is closing.
		var closingVolume = Math.Min(Math.Abs(previousPosition), volume);
		if (closingVolume > 0m)
		{
			var realizedPnL = previousPosition > 0m
				? (price - previousAveragePrice) * closingVolume
				: (previousAveragePrice - price) * closingVolume;

			NotifyOrderClosed(trade, closingVolume, price, previousAveragePrice, realizedPnL);
		}

		// Determine the remaining position after closing part of the exposure.
		var remainingPosition = _currentPosition;
		if (remainingPosition == 0m)
		{
			_averagePrice = 0m;
			return;
		}

		if (Math.Sign(remainingPosition) == Math.Sign(previousPosition))
		{
			// Still holding some quantity in the original direction; keep the historical average price.
			_averagePrice = previousAveragePrice;
			return;
		}

		// Position reversed; the remaining volume is a brand-new exposure in the trade direction.
		_averagePrice = price;
		NotifyOrderOpened(trade, Math.Abs(remainingPosition), price);
	}

	private void NotifyOrderOpened(MyTrade trade, decimal volume, decimal price)
	{
		var subject = "New order";
		var body = BuildOrderMessage(trade, volume, price, null, 0m, false);
		SendNotification(subject, body);
	}

	private void NotifyOrderClosed(MyTrade trade, decimal volume, decimal price, decimal entryPrice, decimal realizedPnL)
	{
		var subject = "Closed order";
		var body = BuildOrderMessage(trade, volume, price, entryPrice, realizedPnL, true);
		SendNotification(subject, body);
	}

	private string BuildOrderMessage(MyTrade trade, decimal volume, decimal price, decimal? entryPrice, decimal realizedPnL, bool isClosing)
	{
		var sb = new StringBuilder();

		var securityId = trade.Order.Security?.Id?.SecurityCode ?? trade.Order.Security?.Id?.ToString() ?? Security?.Id?.SecurityCode ?? Security?.Id?.ToString() ?? "Unknown";
		var sideText = trade.Order.Side == Sides.Buy ? "BUY" : "SELL";
		sb.AppendLine($"{securityId} {sideText} {volume:0.####} @ {price:0.####}");

		if (isClosing && entryPrice.HasValue)
		{
			sb.AppendLine($"Entry price: {entryPrice.Value:0.####}");
			sb.AppendLine($"Realized PnL: {realizedPnL:0.##}");
		}

		sb.AppendLine(BuildAccountDetails());

		return sb.ToString();
	}

	private string BuildAccountDetails()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return "Account information is unavailable.";

		var balance = portfolio.CurrentValue ?? 0m;
		var profit = PnL;
		return $"Account: {portfolio.Name}, balance: {balance:0.##}, profit: {profit:0.##}";
	}

	private void SendNotification(string subject, string body)
	{
		LogInfo($"{subject}: {body.Replace(Environment.NewLine, " | ")}");

		if (!SendEmails)
			return;

		if (string.IsNullOrWhiteSpace(SmtpHost) || string.IsNullOrWhiteSpace(EmailFrom) || string.IsNullOrWhiteSpace(EmailTo))
		{
			LogWarning("SMTP settings are incomplete. Email notification skipped.");
			return;
		}

		try
		{
			using var message = new MailMessage(EmailFrom, EmailTo)
			{
				Subject = SubjectPrefix + subject,
				Body = body
			};

			using var client = new SmtpClient(SmtpHost, SmtpPort)
			{
				EnableSsl = SmtpUseSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};

			if (!string.IsNullOrEmpty(SmtpUser))
			{
				client.Credentials = new NetworkCredential(SmtpUser, SmtpPassword ?? string.Empty);
			}

			client.Send(message);
		}
		catch (Exception ex)
		{
			LogError($"Failed to send email notification: {ex.Message}");
		}
	}
}

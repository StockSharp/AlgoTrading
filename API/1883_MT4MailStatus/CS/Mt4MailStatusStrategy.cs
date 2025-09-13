using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sends periodic email reports about active orders.
/// </summary>
public class Mt4MailStatusStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _sendInterval;
	private readonly StrategyParam<string> _smtpHost;
	private readonly StrategyParam<int> _smtpPort;
	private readonly StrategyParam<string> _smtpUser;
	private readonly StrategyParam<string> _smtpPassword;
	private readonly StrategyParam<string> _fromEmail;
	private readonly StrategyParam<string> _toEmail;

	private DateTimeOffset _lastReportTime;
	private Timer? _timer;

	/// <summary>
	/// Interval between report emails.
	/// </summary>
	public TimeSpan SendInterval { get => _sendInterval.Value; set => _sendInterval.Value = value; }

	/// <summary>
	/// SMTP server hostname.
	/// </summary>
	public string SmtpHost { get => _smtpHost.Value; set => _smtpHost.Value = value; }

	/// <summary>
	/// SMTP server port.
	/// </summary>
	public int SmtpPort { get => _smtpPort.Value; set => _smtpPort.Value = value; }

	/// <summary>
	/// SMTP user name.
	/// </summary>
	public string SmtpUser { get => _smtpUser.Value; set => _smtpUser.Value = value; }

	/// <summary>
	/// SMTP password.
	/// </summary>
	public string SmtpPassword { get => _smtpPassword.Value; set => _smtpPassword.Value = value; }

	/// <summary>
	/// Sender email address.
	/// </summary>
	public string FromEmail { get => _fromEmail.Value; set => _fromEmail.Value = value; }

	/// <summary>
	/// Recipient email address.
	/// </summary>
	public string ToEmail { get => _toEmail.Value; set => _toEmail.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Mt4MailStatusStrategy"/> class.
	/// </summary>
	public Mt4MailStatusStrategy()
	{
		_sendInterval = Param(nameof(SendInterval), TimeSpan.FromHours(1))
			.SetDisplay("Send Interval", "Interval between status reports", "General");

		_smtpHost = Param(nameof(SmtpHost), "smtp.example.com")
			.SetDisplay("SMTP Host", "SMTP server host", "Email");

		_smtpPort = Param(nameof(SmtpPort), 587)
			.SetDisplay("SMTP Port", "SMTP server port", "Email");

		_smtpUser = Param(nameof(SmtpUser), "user@example.com")
			.SetDisplay("SMTP User", "SMTP user name", "Email");

		_smtpPassword = Param(nameof(SmtpPassword), string.Empty)
			.SetDisplay("SMTP Password", "SMTP password", "Email");

		_fromEmail = Param(nameof(FromEmail), "user@example.com")
			.SetDisplay("From Email", "Sender address", "Email");

		_toEmail = Param(nameof(ToEmail), "recipient@example.com")
			.SetDisplay("To Email", "Recipient address", "Email");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SendInterval.TotalSeconds < 60)
			throw new InvalidOperationException("Send interval should be at least 60 seconds.");

		_lastReportTime = time;

		_timer = new Timer(OnTimer, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		base.OnStopped();
	}

	private void OnTimer(object? state)
	{
		var now = CurrentTime;

		if (!IsEnoughTimePassed(_lastReportTime, now, SendInterval))
			return;

		var activeOrders = Orders.Where(o => o.State == OrderStates.Active).ToArray();
		if (activeOrders.Length == 0)
			return;

		var content = $"{now:O}\n###Orders begin###\n";
		foreach (var order in activeOrders)
		{
			var sec = order.Security;
			var bestAsk = sec.BestAsk?.Price;
			var bestBid = sec.BestBid?.Price;
			var typeStr = order.Type switch
			{
				OrderTypes.Market => order.Side == Sides.Buy ? "buy" : "sell",
				OrderTypes.Limit => order.Side == Sides.Buy ? "buylimit" : "selllimit",
				OrderTypes.Stop => order.Side == Sides.Buy ? "buystop" : "sellstop",
				_ => "unknown"
			};

			content += $"Ticket:{order.Id} {sec.Id} {typeStr} at {order.Price}\n" +
				$"SL:{order.StopPrice} TP:{order.TakeProfit}\n" +
				$"Ask:{bestAsk} Bid:{bestBid}\n" +
				$"Volume:{order.Volume}\n\n";
		}
		content += "###Orders end###\n";

		if (SendMail(content))
			_lastReportTime = now;
	}

	private bool SendMail(string body)
	{
		try
		{
			using var client = new SmtpClient(SmtpHost, SmtpPort)
			{
				Credentials = new NetworkCredential(SmtpUser, SmtpPassword),
				EnableSsl = true
			};
			var message = new MailMessage(FromEmail, ToEmail, "Status Reports", body);
			client.Send(message);
			return true;
		}
		catch (Exception ex)
		{
			WriteToLogFile($"SendMail failed error: {ex.Message}");
			return false;
		}
	}

	private static bool IsEnoughTimePassed(DateTimeOffset oldTime, DateTimeOffset newTime, TimeSpan interval)
		=> newTime > oldTime && (newTime - oldTime) > interval;

	private static void WriteToLogFile(string text)
	{
		var fileName = $"mylog_{DateTime.Today:yyyy_MM_dd}.txt";
		File.AppendAllText(fileName, text + Environment.NewLine);
	}
}

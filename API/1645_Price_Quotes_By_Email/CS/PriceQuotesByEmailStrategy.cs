using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Timers;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that sends account information and latest quotes via email.
/// </summary>
public class PriceQuotesByEmailStrategy : Strategy
{
	private readonly StrategyParam<int> _sendInterval;
	private readonly StrategyParam<string> _symbols;
	private readonly StrategyParam<string> _emailFrom;
	private readonly StrategyParam<string> _emailTo;
	private readonly StrategyParam<string> _smtpHost;
	private readonly StrategyParam<int> _smtpPort;
	private readonly StrategyParam<string> _smtpUser;
	private readonly StrategyParam<string> _smtpPassword;

	private readonly Dictionary<Security, (decimal lastPrice, decimal prevClose)> _prices = new();
	private Timer _timer;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public PriceQuotesByEmailStrategy()
	{
	    _sendInterval = Param(nameof(SendInterval), 0)
	        .SetDisplay("Send Interval (min)", "Email frequency in minutes", "General");

	    _symbols = Param(nameof(Symbols), "EURUSD,GBPUSD,USDCHF,USDJPY")
	        .SetDisplay("Symbols (comma separated)", "Instruments to report", "General");

	    _emailFrom = Param(nameof(EmailFrom), "")
	        .SetDisplay("Email From", "Sender address", "Email")
	        .SetCanOptimize(false);

	    _emailTo = Param(nameof(EmailTo), "")
	        .SetDisplay("Email To", "Recipient address", "Email")
	        .SetCanOptimize(false);

	    _smtpHost = Param(nameof(SmtpHost), "")
	        .SetDisplay("SMTP Host", "SMTP server", "Email")
	        .SetCanOptimize(false);

	    _smtpPort = Param(nameof(SmtpPort), 25)
	        .SetDisplay("SMTP Port", "SMTP server port", "Email")
	        .SetCanOptimize(false);

	    _smtpUser = Param(nameof(SmtpUser), "")
	        .SetDisplay("SMTP User", "SMTP login", "Email")
	        .SetCanOptimize(false);

	    _smtpPassword = Param(nameof(SmtpPassword), "")
	        .SetDisplay("SMTP Password", "SMTP password", "Email")
	        .SetCanOptimize(false);
	}

	/// <summary>
	/// Email sending interval in minutes.
	/// </summary>
	public int SendInterval
	{
	    get => _sendInterval.Value;
	    set => _sendInterval.Value = value;
	}

	/// <summary>
	/// Comma separated list of symbols.
	/// </summary>
	public string Symbols
	{
	    get => _symbols.Value;
	    set => _symbols.Value = value;
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
	/// SMTP server address.
	/// </summary>
	public string SmtpHost
	{
	    get => _smtpHost.Value;
	    set => _smtpHost.Value = value;
	}

	/// <summary>
	/// SMTP server port.
	/// </summary>
	public int SmtpPort
	{
	    get => _smtpPort.Value;
	    set => _smtpPort.Value = value;
	}

	/// <summary>
	/// SMTP login name.
	/// </summary>
	public string SmtpUser
	{
	    get => _smtpUser.Value;
	    set => _smtpUser.Value = value;
	}

	/// <summary>
	/// SMTP password.
	/// </summary>
	public string SmtpPassword
	{
	    get => _smtpPassword.Value;
	    set => _smtpPassword.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	    foreach (var symbol in Symbols.Split(',', StringSplitOptions.RemoveEmptyEntries))
	    {
	        var security = this.GetSecurity(symbol.Trim());
	        yield return (security, DataType.Level1);
	    }
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    foreach (var (security, _) in GetWorkingSecurities())
	    {
	        SubscribeLevel1(security)
	            .Bind(ProcessLevel1)
	            .Start();
	    }

	    if (SendInterval <= 0)
	        return;

	    _timer = new Timer(TimeSpan.FromMinutes(SendInterval).TotalMilliseconds)
	    {
	        AutoReset = true,
	    };
	    _timer.Elapsed += (_, _) => SendEmail();
	    _timer.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
	    var security = this.GetSecurity(level1.SecurityId);
	    var tuple = _prices.TryGetValue(security, out var value) ? value : (0m, 0m);

	    if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
	        tuple.lastPrice = (decimal)last;

	    if (level1.Changes.TryGetValue(Level1Fields.ClosePrice, out var close))
	        tuple.prevClose = (decimal)close;

	    _prices[security] = tuple;
	}

	private void SendEmail()
	{
	    var sb = new StringBuilder();
	    sb.AppendLine("My Account Information:");
	    sb.AppendLine();

	    if (Portfolio != null)
	    {
	        sb.AppendLine($"Account Balance:\t{Portfolio.BeginValue}");
	        sb.AppendLine($"Account Equity:\t{Portfolio.CurrentValue}");
	        sb.AppendLine($"Account Profit:\t{Portfolio.CurrentProfit}");
	        sb.AppendLine($"Account Margin:\t{Portfolio.BlockedValue}");
	    }

	    sb.AppendLine();
	    sb.AppendLine("Last Bid Price (Percent Change between Last Price and Yesterday Close Price):");

	    foreach (var pair in _prices)
	    {
	        var last = pair.Value.lastPrice;
	        var prev = pair.Value.prevClose;
	        var change = prev != 0m ? (last / prev - 1m) * 100m : 0m;

	        sb.AppendLine($"{pair.Key.Id}: {last} ({change:+0.##;-0.##;0}%)");
	    }

	    if (string.IsNullOrEmpty(SmtpHost) || string.IsNullOrEmpty(EmailFrom) || string.IsNullOrEmpty(EmailTo))
	        return;

	    using var message = new MailMessage(EmailFrom, EmailTo)
	    {
	        Subject = $"New Quotes on Date: {DateTime.Now:yyyy-MM-dd} Time: {DateTime.Now:HH:mm}",
	        Body = sb.ToString()
	    };

	    using var client = new SmtpClient(SmtpHost, SmtpPort)
	    {
	        EnableSsl = true,
	        Credentials = new NetworkCredential(SmtpUser, SmtpPassword)
	    };

	    client.Send(message);
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time, Exception error)
	{
	    base.OnStopped(time, error);
	    _timer?.Stop();
	    _timer?.Dispose();
	}
}

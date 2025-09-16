using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates the position size allowed by a risk percentage over free margin.
/// Mirrors the MetaTrader fixed margin money management helper by logging the allowed volume for buy and sell operations.
/// </summary>
public class RiskFixedMarginStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;

	private decimal _bestBid;
	private decimal _bestAsk;
	private string _lastStatus;

	/// <summary>
	/// Risk percentage applied to the estimated free capital.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public RiskFixedMarginStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk percent taken from free margin", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_lastStatus = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to Level1 data to receive bid/ask updates required for risk calculations.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// Track the best bid and ask prices from the feed.
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		// Avoid processing until at least one side of the book is known.
		if (_bestBid <= 0m && _bestAsk <= 0m)
			return;

		var status = BuildStatusMessage();
		if (string.IsNullOrEmpty(status) || status == _lastStatus)
			return;

		_lastStatus = status;
		LogInfo(status);
	}

	private string BuildStatusMessage()
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return string.Empty;

		var midPrice = GetMidPrice();
		var usedMargin = midPrice > 0m ? Math.Abs(Position) * midPrice : 0m;
		var freeMargin = Math.Max(equity - usedMargin, 0m);
		var riskAmount = freeMargin * (RiskPercent / 100m);

		var (buyRaw, buyActual) = CalculateVolumes(_bestAsk, riskAmount);
		var (sellRaw, sellActual) = CalculateVolumes(_bestBid, riskAmount);

		var balance = equity - PnL;

		var sb = new StringBuilder();
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:0.##}% risk of free margin", RiskPercent));
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
			"Check open BUY: {0:F2}, Balance: {1:F2}, Equity: {2:F2}, FreeMargin: {3:F2}",
			buyRaw, balance, equity, freeMargin));
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "trade BUY, volume: {0:F2}", buyActual));
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
			"Check open SELL: {0:F2}, Balance: {1:F2}, Equity: {2:F2}, FreeMargin: {3:F2}",
			sellRaw, balance, equity, freeMargin));
		sb.Append(string.Format(CultureInfo.InvariantCulture, "trade SELL, volume: {0:F2}", sellActual));

		return sb.ToString();
	}

	private (decimal raw, decimal adjusted) CalculateVolumes(decimal price, decimal riskAmount)
	{
		// Without a valid price or risk budget we cannot compute size recommendations.
		if (price <= 0m || riskAmount <= 0m)
			return (0m, 0m);

		var rawVolume = riskAmount / price;
		var adjustedVolume = NormalizeVolume(rawVolume);
		return (rawVolume, adjustedVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var multiples = Math.Floor(volume / step);
		var normalized = multiples * step;
		return Math.Max(normalized, 0m);
	}

	private decimal GetMidPrice()
	{
		if (_bestBid > 0m && _bestAsk > 0m)
			return (_bestBid + _bestAsk) / 2m;

		return Math.Max(_bestBid, _bestAsk);
	}
}

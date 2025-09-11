using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Screener strategy that evaluates RSI across multiple symbols and logs ratings.
/// </summary>
public class CleanerScreenersLibraryStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _strongThreshold;
	private readonly StrategyParam<decimal> _weakThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Upper threshold for strong rating.
	/// </summary>
	public decimal StrongThreshold { get => _strongThreshold.Value; set => _strongThreshold.Value = value; }

	/// <summary>
	/// Threshold for buy rating.
	/// </summary>
	public decimal WeakThreshold { get => _weakThreshold.Value; set => _weakThreshold.Value = value; }

	/// <summary>
	/// Candle type used for all symbols.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Collection of symbols to screen.
	/// </summary>
	public IList<Security> Symbols { get; } = new List<Security>();

	/// <summary>
	/// Initializes a new instance of the <see cref="CleanerScreenersLibraryStrategy"/> class.
	/// </summary>
	public CleanerScreenersLibraryStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI indicator", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_strongThreshold = Param(nameof(StrongThreshold), 70m)
			.SetDisplay("Strong Threshold", "Upper RSI threshold for strong rating", "General")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_weakThreshold = Param(nameof(WeakThreshold), 60m)
			.SetDisplay("Weak Threshold", "RSI threshold for buy rating", "General")
			.SetCanOptimize(true)
			.SetOptimize(40m, 70m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in Symbols)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var security in Symbols)
		{
			var rsi = new RSI { Length = RsiLength };
			var subscription = SubscribeCandles(CandleType, security: security);

			subscription
				.Bind(rsi, (candle, rsiValue) =>
				{
					if (candle.State != CandleStates.Finished)
						return;

					var rating = GetRating(rsiValue);
					LogInfo($"{security.Id} {rating} (RSI {rsiValue:F2})");
				})
				.Start();
		}
	}

	private string GetRating(decimal value)
	{
		if (value >= StrongThreshold)
			return "Strong Buy";

		if (value >= WeakThreshold)
			return "Buy";

		if (value <= 100m - StrongThreshold)
			return "Strong Sell";

		if (value <= 100m - WeakThreshold)
			return "Sell";

		return "Neutral";
	}
}

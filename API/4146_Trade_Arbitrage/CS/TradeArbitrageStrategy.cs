using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangular arbitrage strategy converted from the MetaTrader "Trade-Arbitrage" expert advisor.
/// The strategy reconstructs every possible synthetic quote for the configured currency list and
/// opens hedged baskets whenever a synthetic rate can be traded against another with an edge larger
/// than <see cref="MinimumEdgePips"/>.
/// </summary>
public class TradeArbitrageStrategy : Strategy
{
	private readonly StrategyParam<string> _currenciesParam;
	private readonly StrategyParam<decimal> _minimumEdgePipsParam;
	private readonly StrategyParam<decimal> _lotSizeParam;
	private readonly StrategyParam<decimal> _minLotParam;
	private readonly StrategyParam<decimal> _maxLotParam;
	private readonly StrategyParam<string> _allowedPatternsParam;
	private readonly StrategyParam<string> _symbolSuffixParam;
	private readonly StrategyParam<decimal> _alphaParam;

	private readonly Dictionary<Security, Quote> _quotes = new();
	private readonly List<Combination> _combinations = new();
	private readonly Dictionary<Security, decimal> _pendingVolumes = new();
	private readonly HashSet<string> _allowedPatterns = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<Security> _uniqueSecurities = new();
	private readonly List<Security> _subscriptionOrder = new();

	private bool _graphNeedsRebuild = true;

	/// <summary>
	/// List of currencies used to build the arbitrage graph.
	/// Provide a comma separated list of ISO currency codes.
	/// </summary>
	public string Currencies
	{
		get => _currenciesParam.Value;
		set
		{
			_currenciesParam.Value = value;
			_graphNeedsRebuild = true;
		}
	}

	/// <summary>
	/// Minimal price advantage expressed in pips before a basket is opened.
	/// </summary>
	public decimal MinimumEdgePips
	{
		get => _minimumEdgePipsParam.Value;
		set => _minimumEdgePipsParam.Value = value;
	}

	/// <summary>
	/// Base order size used when opening new arbitrage baskets.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSizeParam.Value;
		set => _lotSizeParam.Value = value;
	}

	/// <summary>
	/// Minimal trade volume accepted by the strategy.
	/// Orders below this amount are ignored.
	/// </summary>
	public decimal MinLot
	{
		get => _minLotParam.Value;
		set => _minLotParam.Value = value;
	}

	/// <summary>
	/// Maximal trade volume that can be sent in a single order.
	/// Larger baskets are split into several chunks.
	/// </summary>
	public decimal MaxLot
	{
		get => _maxLotParam.Value;
		set => _maxLotParam.Value = value;
	}

	/// <summary>
	/// Optional whitelist of symbol combinations that may trade.
	/// Lines must follow the "A && B" format identical to the MetaTrader text file.
	/// </summary>
	public string AllowedPatterns
	{
		get => _allowedPatternsParam.Value;
		set
		{
			_allowedPatternsParam.Value = value;
			_graphNeedsRebuild = true;
		}
	}

	/// <summary>
	/// Optional symbol suffix appended to every generated currency pair.
	/// Leave empty when the provider exposes plain six letter codes.
	/// </summary>
	public string SymbolSuffix
	{
		get => _symbolSuffixParam.Value;
		set
		{
			_symbolSuffixParam.Value = value;
			_graphNeedsRebuild = true;
		}
	}

	/// <summary>
	/// Smoothing coefficient applied when distributing the basket volume across hedged legs.
	/// </summary>
	public decimal Alpha
	{
		get => _alphaParam.Value;
		set => _alphaParam.Value = value;
	}

	/// <summary>
	/// Initializes parameters for <see cref="TradeArbitrageStrategy"/>.
	/// </summary>
	public TradeArbitrageStrategy()
	{
		_currenciesParam = Param(nameof(Currencies), "AUD, EUR, USD, CHF, JPY, NZD, GBP, CAD, SGD, NOK, SEK, DKK, ZAR, MXN, HKD, HUF, CZK, PLN, RUR, TRY")
		.SetDisplay("Currencies", "Comma separated ISO currency codes used to build synthetic pairs", "General");

		_minimumEdgePipsParam = Param(nameof(MinimumEdgePips), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Min Edge (pips)", "Minimal arbitrage edge in pips before a trade is triggered", "Risk");

		_lotSizeParam = Param(nameof(LotSize), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Base lot size for each arbitrage basket", "Position Sizing")
		.SetCanOptimize(true);

		_minLotParam = Param(nameof(MinLot), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Min Lot", "Minimal allowed order volume", "Position Sizing");

		_maxLotParam = Param(nameof(MaxLot), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Max Lot", "Maximum volume for a single order", "Position Sizing");

		_allowedPatternsParam = Param(nameof(AllowedPatterns), string.Empty)
		.SetDisplay("Allowed Patterns", "Optional whitelist of combinations in the format 'A && B'", "Filters");

		_symbolSuffixParam = Param(nameof(SymbolSuffix), string.Empty)
		.SetDisplay("Symbol Suffix", "Suffix appended to every generated currency pair", "Connectivity");

		_alphaParam = Param(nameof(Alpha), 0.001m)
		.SetGreaterThanZero()
		.SetDisplay("Alpha", "Smoothing coefficient used when distributing basket volume", "Execution");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in _subscriptionOrder)
		yield return (security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_quotes.Clear();
		_combinations.Clear();
		_pendingVolumes.Clear();
		_allowedPatterns.Clear();
		_uniqueSecurities.Clear();
		_subscriptionOrder.Clear();
		_graphNeedsRebuild = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (_graphNeedsRebuild)
		PrepareArbitrageGraph();

		if (_combinations.Count == 0)
		throw new InvalidOperationException("No arbitrage combinations could be constructed for the current settings.");

		foreach (var security in _subscriptionOrder)
		{
			var localSecurity = security;

			SubscribeLevel1(localSecurity)
			.Bind(message => OnLevel1(localSecurity, message))
			.Start();
		}
	}

	private void OnLevel1(Security security, Level1ChangeMessage message)
	{
		if (!_quotes.TryGetValue(security, out var quote))
		return;

		var updated = false;

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid.HasValue)
		{
			quote.Bid = bid.Value;
			updated = true;
		}

		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask.HasValue)
		{
			quote.Ask = ask.Value;
			updated = true;
		}

		if (!updated)
		return;

		EvaluateArbitrage();
	}

	private void EvaluateArbitrage()
	{
		if (!IsActive)
		return;

		_pendingVolumes.Clear();

		foreach (var combination in _combinations)
		{
			var count = combination.Variants.Count;
			if (count < 2)
			continue;

		decimal referencePrice = 0;
		var referenceReady = false;

		for (var index = 0; index < count; index++)
		{
			var variant = combination.Variants[index];
			if (TryComputeVariantQuotes(variant, out var bid, out var ask))
			{
				variant.LastBid = bid;
				variant.LastAsk = ask;

				if (!referenceReady && bid > 0)
				{
					referencePrice = bid;
					referenceReady = true;
				}
			}
			else
			{
				variant.LastBid = null;
				variant.LastAsk = null;
			}
		}

		if (referenceReady)
		{
			combination.Point = referencePrice > 10m ? 0.01m : 0.0001m;
			combination.MinEdge = MinimumEdgePips * combination.Point;
		}

		for (var left = 0; left < count - 1; left++)
		{
			var leftVariant = combination.Variants[left];
			if (leftVariant.LastBid is not decimal leftBid || leftVariant.LastAsk is not decimal leftAsk)
			continue;

		for (var right = left + 1; right < count; right++)
		{
			var rightVariant = combination.Variants[right];
			if (rightVariant.LastBid is not decimal rightBid || rightVariant.LastAsk is not decimal rightAsk)
			continue;

		var adjustedBid = leftBid - combination.MinEdge;
		var adjustedAsk = leftAsk + combination.MinEdge;

		if (adjustedBid > rightAsk)
		{
			TryExecuteArbitrage(combination, left, right, LotSize);
		}
		else if (adjustedAsk < rightBid)
		{
			TryExecuteArbitrage(combination, right, left, LotSize);
		}
	}
}
}

ExecutePendingPositions();
}

private bool TryComputeVariantQuotes(Variant variant, out decimal bid, out decimal ask)
{
	bid = 0;
	ask = 0;

	switch (variant.Math)
		{
			case VariantMaths.Direct:
				{
					return TryGetQuote(variant.PrimarySecurity, out bid, out ask);
				}

				case VariantMaths.Inverse:
					{
						if (!TryGetQuote(variant.PrimarySecurity, out var baseBid, out var baseAsk))
						return false;

						if (baseBid <= 0m || baseAsk <= 0m)
						return false;

						bid = 1m / baseAsk;
						ask = 1m / baseBid;
						return true;
					}

					case VariantMaths.Ratio:
						{
							if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
							return false;

							if (!TryGetQuote(variant.SecondarySecurity, out var bid2, out var ask2))
							return false;

							if (bid2 <= 0m || ask2 <= 0m)
							return false;

							bid = bid1 / ask2;
							ask = ask1 / bid2;
							return true;
						}

						case VariantMaths.Product:
							{
								if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
								return false;

								if (!TryGetQuote(variant.SecondarySecurity, out var bid2, out var ask2))
								return false;

								bid = bid1 * bid2;
								ask = ask1 * ask2;
								return true;
							}

							case VariantMaths.InverseProduct:
								{
									if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
									return false;

									if (!TryGetQuote(variant.SecondarySecurity, out var bid2, out var ask2))
									return false;

									var askProduct = ask1 * ask2;
									var bidProduct = bid1 * bid2;

									if (askProduct <= 0m || bidProduct <= 0m)
									return false;

									bid = 1m / askProduct;
									ask = 1m / bidProduct;
									return true;
								}

								case VariantMaths.ReverseRatio:
									{
										if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
										return false;

										if (!TryGetQuote(variant.SecondarySecurity, out var bid2, out var ask2))
										return false;

										if (bid1 <= 0m || ask1 <= 0m)
										return false;

										bid = bid2 / ask1;
										ask = ask2 / bid1;
										return true;
									}
								}

								return false;
							}

							private void TryExecuteArbitrage(Combination combination, int sellIndex, int buyIndex, decimal baseVolume)
							{
								if (baseVolume <= 0m)
								return;

								if (!IsPairAllowed(combination, sellIndex, buyIndex))
								return;

								var key = sellIndex < buyIndex ? (sellIndex, buyIndex) : (buyIndex, sellIndex);
								combination.NetPositions.TryGetValue(key, out var state);

								decimal tradeVolume;

								if (sellIndex < buyIndex)
								{
									if (state < -Alpha)
									return;

									tradeVolume = state + baseVolume;
									combination.NetPositions[key] = -baseVolume;
								}
								else
								{
									if (state > Alpha)
									return;

									tradeVolume = baseVolume - state;
									combination.NetPositions[key] = baseVolume;
								}

								if (tradeVolume <= 0m)
								return;

								DispatchBasket(combination.Variants[sellIndex], combination.Variants[buyIndex], tradeVolume);
							}

							private void DispatchBasket(Variant sellVariant, Variant buyVariant, decimal volume)
							{
								var remaining = volume;

								while (remaining > 0m)
								{
									var chunk = Math.Min(remaining, MaxLot);
									if (chunk < MinLot)
									break;

								ExecuteVariantTrade(sellVariant, Sides.Sell, chunk);
								ExecuteVariantTrade(buyVariant, Sides.Buy, chunk);

								remaining -= chunk;
							}
						}

						private void ExecuteVariantTrade(Variant variant, Sides side, decimal volume)
						{
							if (volume <= 0m)
							return;

							switch (variant.Math)
								{
									case VariantMaths.Direct:
										{
											AddPendingVolume(variant.PrimarySecurity, side == Sides.Buy ? volume : -volume);
											break;
									}

									case VariantMaths.Inverse:
										{
											if (!TryGetQuote(variant.PrimarySecurity, out var bid, out var ask))
											return;

											if (side == Sides.Sell)
											{
												if (ask <= 0m)
												return;

												var converted = volume / ask;
												AddPendingVolume(variant.PrimarySecurity, converted);
											}
											else
											{
												if (bid <= 0m)
												return;

												var converted = -volume / bid;
												AddPendingVolume(variant.PrimarySecurity, converted);
											}

											break;
									}

									case VariantMaths.Ratio:
										{
											if (variant.LastBid is not decimal lastBid || variant.LastAsk is not decimal lastAsk)
											return;

											if (side == Sides.Sell)
											{
												AddPendingVolume(variant.PrimarySecurity, -volume);
												AddPendingVolume(variant.SecondarySecurity, volume * lastBid);
											}
											else
											{
												AddPendingVolume(variant.PrimarySecurity, volume);
												AddPendingVolume(variant.SecondarySecurity, -volume * lastAsk);
											}

											break;
									}

									case VariantMaths.Product:
										{
											if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
											return;

											if (side == Sides.Sell)
											{
												AddPendingVolume(variant.PrimarySecurity, -volume);
												AddPendingVolume(variant.SecondarySecurity, -volume * bid1);
											}
											else
											{
												AddPendingVolume(variant.PrimarySecurity, volume);
												AddPendingVolume(variant.SecondarySecurity, volume * ask1);
											}

											break;
									}

									case VariantMaths.InverseProduct:
										{
											if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
											return;

											if (variant.LastBid is not decimal lastBid || variant.LastAsk is not decimal lastAsk)
											return;

											if (side == Sides.Sell)
											{
												if (ask1 <= 0m)
												return;

												AddPendingVolume(variant.PrimarySecurity, volume / ask1);
												AddPendingVolume(variant.SecondarySecurity, volume * lastBid);
											}
											else
											{
												if (bid1 <= 0m)
												return;

												AddPendingVolume(variant.PrimarySecurity, -volume / bid1);
												AddPendingVolume(variant.SecondarySecurity, -volume * lastAsk);
											}

											break;
									}

									case VariantMaths.ReverseRatio:
										{
											if (!TryGetQuote(variant.PrimarySecurity, out var bid1, out var ask1))
											return;

											if (side == Sides.Sell)
											{
												if (ask1 <= 0m)
												return;

												var converted = volume / ask1;
												AddPendingVolume(variant.PrimarySecurity, converted);
												AddPendingVolume(variant.SecondarySecurity, -converted);
											}
											else
											{
												if (bid1 <= 0m)
												return;

												var converted = -volume / bid1;
												AddPendingVolume(variant.PrimarySecurity, converted);
												AddPendingVolume(variant.SecondarySecurity, -converted);
											}

											break;
									}
								}
							}

							private void AddPendingVolume(Security security, decimal volume)
							{
								if (security == null)
								return;

								if (Math.Abs(volume) < 1e-8m)
								return;

								if (_pendingVolumes.TryGetValue(security, out var existing))
								_pendingVolumes[security] = existing + volume;
								else
								_pendingVolumes.Add(security, volume);
							}

							private void ExecutePendingPositions()
							{
								foreach (var pair in _pendingVolumes)
								{
									var security = pair.Key;
									var volume = pair.Value;

									if (Math.Abs(volume) < MinLot)
									continue;

								var step = security.VolumeStep ?? 0.01m;
								if (step <= 0m)
								step = 0.01m;

								var minVolume = security.MinVolume ?? MinLot;
								if (minVolume < MinLot)
								minVolume = MinLot;

								var maxVolume = security.MaxVolume ?? MaxLot;
								if (maxVolume > MaxLot)
								maxVolume = MaxLot;

								var remaining = volume;

								while (Math.Abs(remaining) >= minVolume)
								{
									var chunk = Math.Min(Math.Abs(remaining), maxVolume);
									chunk = Math.Truncate(chunk / step) * step;

									if (chunk < minVolume)
									break;

								if (remaining > 0m)
								BuyMarket(chunk, security);
								else
								SellMarket(chunk, security);

								remaining += remaining > 0m ? -chunk : chunk;
							}
						}

						_pendingVolumes.Clear();
					}

					private bool TryGetQuote(Security security, out decimal bid, out decimal ask)
					{
						bid = 0;
						ask = 0;

						if (!_quotes.TryGetValue(security, out var quote))
						return false;

						if (!quote.Bid.HasValue || !quote.Ask.HasValue)
						return false;

						bid = quote.Bid.Value;
						ask = quote.Ask.Value;
						return true;
					}

					private bool IsPairAllowed(Combination combination, int leftIndex, int rightIndex)
					{
						if (_allowedPatterns.Count == 0)
						return true;

						var leftName = GetVariantDescription(combination, leftIndex);
						var rightName = GetVariantDescription(combination, rightIndex);

						var pattern = $"{leftName} && {rightName}";
						if (_allowedPatterns.Contains(pattern))
						return true;

						var reversed = $"{rightName} && {leftName}";
						return _allowedPatterns.Contains(reversed);
					}

					private string GetVariantDescription(Combination combination, int variantIndex)
					{
						var variant = combination.Variants[variantIndex];

						return variant.Math switch
						{
							VariantMaths.Direct => variant.PrimaryCode,
							VariantMaths.Inverse => $"1 / {variant.PrimaryCode}",
							VariantMaths.Ratio => $"{variant.PrimaryCode} / {variant.SecondaryCode}",
							VariantMaths.Product => $"{variant.PrimaryCode} * {variant.SecondaryCode}",
							VariantMaths.InverseProduct => $"1 / ({variant.PrimaryCode} * {variant.SecondaryCode})",
							VariantMaths.ReverseRatio => $"{variant.SecondaryCode} / {variant.PrimaryCode}",
							_ => variant.PrimaryCode,
						};
					}

					private void PrepareArbitrageGraph()
					{
						_quotes.Clear();
						_combinations.Clear();
						_pendingVolumes.Clear();
						_allowedPatterns.Clear();
						_uniqueSecurities.Clear();
						_subscriptionOrder.Clear();

						ParseAllowedPatterns();

						var currencies = ParseCurrencies();
						if (currencies.Count < 2)
						throw new InvalidOperationException("At least two currencies are required to build arbitrage combinations.");

						var provider = SecurityProvider;
						if (provider == null)
						throw new InvalidOperationException("Security provider is not configured.");

if (SymbolSuffix.IsEmpty() && Security?.Code is string code && code.Length > 6)
{
var suffix = code.Substring(6);
if (!suffix.IsEmpty())
SymbolSuffix = suffix;
}

						var resolved = new Dictionary<string, Security>(StringComparer.OrdinalIgnoreCase);

						foreach (var baseCurrency in currencies)
						{
							foreach (var quoteCurrency in currencies)
							{
								if (baseCurrency.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase))
								continue;

							var symbol = baseCurrency + quoteCurrency;
							if (resolved.ContainsKey(symbol))
							continue;

						var security = ResolveSecurity(provider, symbol);
						if (security == null)
						{
							LogWarning($"Security '{symbol}' could not be resolved.");
							continue;
					}

					resolved.Add(symbol, security);
					EnsureQuote(security);
				}
			}

			foreach (var baseCurrency in currencies)
			{
				foreach (var quoteCurrency in currencies)
				{
					if (baseCurrency.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase))
					continue;

				var combination = new Combination(baseCurrency, quoteCurrency);

				var direct = CreateDirectVariant(resolved, baseCurrency + quoteCurrency, VariantMaths.Direct);
				if (direct != null)
				combination.Variants.Add(direct);

				var inverse = CreateDirectVariant(resolved, quoteCurrency + baseCurrency, VariantMaths.Inverse);
				if (inverse != null)
				combination.Variants.Add(inverse);

				foreach (var cross in currencies)
				{
					if (cross.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase) || cross.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase))
					continue;

				TryAddCrossVariant(combination, resolved, baseCurrency + cross, quoteCurrency + cross, VariantMaths.Ratio);
				TryAddCrossVariant(combination, resolved, baseCurrency + cross, cross + quoteCurrency, VariantMaths.Product);
				TryAddCrossVariant(combination, resolved, cross + baseCurrency, quoteCurrency + cross, VariantMaths.InverseProduct);
				TryAddCrossVariant(combination, resolved, cross + baseCurrency, cross + quoteCurrency, VariantMaths.ReverseRatio);
			}

			if (combination.Variants.Count >= 2)
			_combinations.Add(combination);
		}
	}

	_graphNeedsRebuild = false;
}

private void ParseAllowedPatterns()
{
	if (AllowedPatterns.IsEmptyOrWhiteSpace())
	return;

	var lines = AllowedPatterns.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
	foreach (var rawLine in lines)
	{
		var line = rawLine.Trim();
		if (line.Length == 0)
		continue;

	if (line.StartsWith("//", StringComparison.Ordinal))
	continue;

_allowedPatterns.Add(line);
}
}

private List<string> ParseCurrencies()
{
	var result = new List<string>();

	if (Currencies.IsEmptyOrWhiteSpace())
	return result;

	var tokens = Currencies.Split(new[] { ',', ';', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

	foreach (var token in tokens)
	{
		var currency = token.Trim().ToUpperInvariant();
		if (currency.Length == 0)
		continue;

	if (!result.Contains(currency))
	result.Add(currency);
}

return result;
}

private Security ResolveSecurity(ISecurityProvider provider, string symbol)
{
	Security security = null;

	if (!SymbolSuffix.IsEmpty())
	{
		security = provider.LookupById(symbol + SymbolSuffix);
	}

	security ??= provider.LookupById(symbol);

	return security;
}

private Variant CreateDirectVariant(Dictionary<string, Security> resolved, string code, VariantMaths math)
{
	if (!resolved.TryGetValue(code, out var security))
	return null;

	EnsureQuote(security);

	return new Variant
	{
		Math = math,
		PrimarySecurity = security,
		PrimaryCode = code
	};
}

private void TryAddCrossVariant(Combination combination, Dictionary<string, Security> resolved, string firstCode, string secondCode, VariantMaths math)
{
	if (!resolved.TryGetValue(firstCode, out var first))
	return;

	if (!resolved.TryGetValue(secondCode, out var second))
	return;

	EnsureQuote(first);
	EnsureQuote(second);

	combination.Variants.Add(new Variant
	{
		Math = math,
		PrimarySecurity = first,
		SecondarySecurity = second,
		PrimaryCode = firstCode,
		SecondaryCode = secondCode
	});
}

private void EnsureQuote(Security security)
{
	if (security == null)
	return;

	if (!_quotes.ContainsKey(security))
	_quotes.Add(security, new Quote());

	if (_uniqueSecurities.Add(security))
	_subscriptionOrder.Add(security);
}

private sealed class Quote
{
	public decimal? Bid { get; set; }
	public decimal? Ask { get; set; }
}

private sealed class Combination
{
	public Combination(string baseCurrency, string quoteCurrency)
	{
		BaseCurrency = baseCurrency;
		QuoteCurrency = quoteCurrency;
	}

	public string BaseCurrency { get; }
	public string QuoteCurrency { get; }
	public List<Variant> Variants { get; } = new();
	public Dictionary<(int left, int right), decimal> NetPositions { get; } = new();
	public decimal Point { get; set; }
	public decimal MinEdge { get; set; }
}

private sealed class Variant
{
	public VariantMaths Math { get; set; }
	public Security PrimarySecurity { get; set; }
	public Security SecondarySecurity { get; set; }
	public string PrimaryCode { get; set; }
	public string SecondaryCode { get; set; }
	public decimal? LastBid { get; set; }
	public decimal? LastAsk { get; set; }
}

private enum VariantMaths
{
	Inverse = -2,
	Direct = -1,
	Ratio = 0,
	Product = 1,
	InverseProduct = 2,
	ReverseRatio = 3
}
}

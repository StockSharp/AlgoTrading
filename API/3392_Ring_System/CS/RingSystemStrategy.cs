using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangular arbitrage strategy inspired by the MT5 Ring System EA.
/// </summary>
public class RingSystemStrategy : Strategy
{
	private readonly StrategyParam<string> _currencies;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _symbolPrefix;
	private readonly StrategyParam<string> _symbolSuffix;
	private readonly StrategyParam<bool> _flattenOnStop;

	private Security? _pairOne;
	private Security? _pairTwo;
	private Security? _pairThree;

	private readonly Dictionary<Security, decimal> _lastCloses = new();
	private int _ringDirection;

	/// <summary>
	/// Initialize <see cref="RingSystemStrategy"/>.
	/// </summary>
	public RingSystemStrategy()
	{
		_currencies = Param(nameof(Currencies), "EUR/GBP/USD")
			.SetDisplay("Currencies", "Ordered list of currencies used to build the triangular ring.", "General");

		_entryThreshold = Param(nameof(EntryThreshold), 0.0005m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Relative imbalance needed to open the ring.", "Trading")
			.SetCanOptimize(true);

		_exitThreshold = Param(nameof(ExitThreshold), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Threshold", "Relative imbalance level required to close the ring.", "Trading")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 10000m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume sent to every leg of the ring.", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle aggregation used to evaluate spreads.", "General");

		_symbolPrefix = Param(nameof(SymbolPrefix), string.Empty)
			.SetDisplay("Symbol Prefix", "Optional prefix applied to every generated symbol.", "General");

		_symbolSuffix = Param(nameof(SymbolSuffix), string.Empty)
			.SetDisplay("Symbol Suffix", "Optional suffix applied to every generated symbol.", "General");

		_flattenOnStop = Param(nameof(FlattenOnStop), true)
			.SetDisplay("Flatten On Stop", "Close all legs automatically when the strategy stops.", "Risk");
	}

	/// <summary>
	/// Ordered currency list that defines the ring.
	/// </summary>
	public string Currencies
	{
		get => _currencies.Value;
		set => _currencies.Value = value;
	}

	/// <summary>
	/// Required relative deviation between theoretical and actual cross rate to enter a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Deviation level at which existing ring positions are closed.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Order volume used for every leg of the ring.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type leveraged to evaluate spreads.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Prefix appended to every generated symbol code.
	/// </summary>
	public string SymbolPrefix
	{
		get => _symbolPrefix.Value;
		set => _symbolPrefix.Value = value;
	}

	/// <summary>
	/// Suffix appended to every generated symbol code.
	/// </summary>
	public string SymbolSuffix
	{
		get => _symbolSuffix.Value;
		set => _symbolSuffix.Value = value;
	}

	/// <summary>
	/// Automatically flatten all ring legs when the strategy is stopped.
	/// </summary>
	public bool FlattenOnStop
	{
		get => _flattenOnStop.Value;
		set => _flattenOnStop.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var currencies = ParseCurrencies();

		var pairSymbols = BuildPairSymbols(currencies);

		_pairOne = ResolveSecurity(pairSymbols[0]);
		_pairTwo = ResolveSecurity(pairSymbols[1]);
		_pairThree = ResolveSecurity(pairSymbols[2]);

		Subscribe(_pairOne);
		Subscribe(_pairTwo);
		Subscribe(_pairThree);

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (FlattenOnStop)
		{
			FlattenRing();
		}

		base.OnStopped();
	}

	private string[] ParseCurrencies()
	{
		var split = Currencies
			.Split(new[] { '/', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(c => c.Trim().ToUpperInvariant())
			.Where(c => !string.IsNullOrWhiteSpace(c))
			.ToArray();

		if (split.Length < 3)
			throw new InvalidOperationException("At least three currencies are required to build a triangular ring.");

		return split[..3];
	}

	private string[] BuildPairSymbols(IReadOnlyList<string> currencies)
	{
		return new[]
		{
			CreateSymbol(currencies[0], currencies[1]),
			CreateSymbol(currencies[1], currencies[2]),
			CreateSymbol(currencies[0], currencies[2])
		};
	}

	private string CreateSymbol(string baseCurrency, string quoteCurrency)
	{
		return string.Concat(SymbolPrefix, baseCurrency, quoteCurrency, SymbolSuffix);
	}

	private Security ResolveSecurity(string symbol)
	{
		var security = this.GetSecurity(symbol);

		return security ?? throw new InvalidOperationException($"Security '{symbol}' is not available in the security provider.");
	}

	private void Subscribe(Security security)
	{
		SubscribeCandles(CandleType, true, security)
			.Bind(candle => ProcessCandle(candle, security))
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastCloses[security] = candle.ClosePrice;

		EvaluateRing();
	}

	private void EvaluateRing()
	{
		if (_pairOne == null || _pairTwo == null || _pairThree == null)
			return;

		if (!_lastCloses.TryGetValue(_pairOne, out var priceOne) ||
			!_lastCloses.TryGetValue(_pairTwo, out var priceTwo) ||
			!_lastCloses.TryGetValue(_pairThree, out var priceThree))
		{
			return;
		}

		if (priceThree == 0m)
			return;

		var theoretical = priceOne * priceTwo;

		if (theoretical == 0m)
			return;

		var deviation = theoretical / priceThree - 1m;

		if (_ringDirection == 0)
		{
			if (deviation >= EntryThreshold)
			{
				EnterLongRing();
			}
			else if (deviation <= -EntryThreshold)
			{
				EnterShortRing();
			}
		}
		else if (Math.Abs(deviation) <= ExitThreshold)
		{
			FlattenRing();
		}
	}

	private void EnterLongRing()
	{
		if (_pairOne == null || _pairTwo == null || _pairThree == null)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		if (!CanOpenRing())
			return;

		// Buy the undervalued cross and short the remaining legs.
		BuyMarket(volume, security: _pairThree);
		SellMarket(volume, security: _pairOne);
		SellMarket(volume, security: _pairTwo);

		_ringDirection = 1;
	}

	private void EnterShortRing()
	{
		if (_pairOne == null || _pairTwo == null || _pairThree == null)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		if (!CanOpenRing())
			return;

		// Sell the overvalued cross and buy the remaining legs.
		SellMarket(volume, security: _pairThree);
		BuyMarket(volume, security: _pairOne);
		BuyMarket(volume, security: _pairTwo);

		_ringDirection = -1;
	}

	private bool CanOpenRing()
	{
		if (_pairOne == null || _pairTwo == null || _pairThree == null)
			return false;

		// Avoid stacking positions if any leg is already open.
		return Math.Abs(GetPosition(_pairOne)) <= 0m &&
			Math.Abs(GetPosition(_pairTwo)) <= 0m &&
			Math.Abs(GetPosition(_pairThree)) <= 0m;
	}

	private void FlattenRing()
	{
		if (_pairOne != null)
		{
			Flatten(_pairOne);
		}

		if (_pairTwo != null)
		{
			Flatten(_pairTwo);
		}

		if (_pairThree != null)
		{
			Flatten(_pairThree);
		}

		_ringDirection = 0;
	}

	private void Flatten(Security security)
	{
		var position = GetPosition(security);

		if (position > 0m)
		{
			SellMarket(position, security: security);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position), security: security);
		}
	}

	private decimal GetPosition(Security security)
	{
		return GetPositionValue(security, Portfolio) ?? 0m;
	}
}

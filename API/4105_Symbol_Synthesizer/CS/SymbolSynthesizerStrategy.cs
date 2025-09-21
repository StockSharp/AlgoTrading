using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Synthetic FX hedging strategy converted from the "SymbolSynthesizer" MQL5 expert.
/// It recreates the original order panel logic by monitoring two leg instruments,
/// computing the synthetic cross quotes, and sending paired orders when requested.
/// </summary>
public class SymbolSynthesizerStrategy : Strategy
{
	private sealed record SyntheticCombination(string SyntheticSymbol, string FirstLeg, string SecondLeg, bool IsProduct);

	private static readonly SyntheticCombination[] _predefinedCombinations =
	{
		new("EURUSD", "EURGBP", "GBPUSD", true),
		new("GBPUSD", "EURGBP", "EURUSD", false),
		new("USDCHF", "EURUSD", "EURCHF", false),
		new("USDJPY", "EURUSD", "EURJPY", false),
		new("USDCAD", "EURUSD", "EURCAD", false),
		new("AUDUSD", "EURAUD", "EURUSD", false),
		new("EURGBP", "GBPUSD", "EURUSD", false),
		new("EURAUD", "AUDUSD", "EURUSD", false),
		new("EURCHF", "EURUSD", "USDCHF", true),
		new("EURJPY", "EURUSD", "USDJPY", true),
		new("GBPJPY", "GBPUSD", "USDJPY", true),
		new("AUDJPY", "AUDUSD", "USDJPY", true),
		new("GBPCHF", "GBPUSD", "USDCHF", true)
	};

	private readonly StrategyParam<int> _combinationParam;
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _slippageParam;
	private readonly StrategyParam<SyntheticTradeAction> _tradeActionParam;

	private SyntheticCombination? _combination;
	private Security? _firstLeg;
	private Security? _secondLeg;

	private decimal? _firstBid;
	private decimal? _firstAsk;
	private decimal? _secondBid;
	private decimal? _secondAsk;
	private decimal? _syntheticBid;
	private decimal? _syntheticAsk;
	private decimal? _lastLoggedBid;
	private decimal? _lastLoggedAsk;

	private bool _isPlacingOrders;

	/// <summary>
	/// Index of the predefined synthetic cross combination.
	/// </summary>
	public int CombinationIndex
	{
		get => _combinationParam.Value;
		set => _combinationParam.Value = value;
	}

	/// <summary>
	/// Initial volume for the first leg (in lots or contracts depending on broker settings).
	/// </summary>
	public decimal OrderVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Maximum slippage allowed in price steps when sending limit orders.
	/// </summary>
	public decimal Slippage
	{
		get => _slippageParam.Value;
		set => _slippageParam.Value = value;
	}

	/// <summary>
	/// Manual action that emulates the Buy/Sell buttons from the original panel.
	/// </summary>
	public SyntheticTradeAction TradeAction
	{
		get => _tradeActionParam.Value;
		set => _tradeActionParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SymbolSynthesizerStrategy"/> class.
	/// </summary>
	public SymbolSynthesizerStrategy()
	{
		_combinationParam = Param(nameof(CombinationIndex), 0)
		.SetDisplay("Combination Index", "Predefined synthetic pair index (0-12)", "Synthetic")
		.SetNotNegative();

		_volumeParam = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Volume for the first leg", "Trading")
		.SetGreaterThanZero();

		_slippageParam = Param(nameof(Slippage), 30m)
		.SetDisplay("Slippage", "Maximum slippage in price steps", "Trading")
		.SetNotNegative();

		_tradeActionParam = Param(nameof(TradeAction), SyntheticTradeAction.None)
		.SetDisplay("Trade Action", "Set to Buy or Sell to place paired orders", "Manual")
		.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		EnsureCombinationInitialized(false);

		if (_firstLeg != null)
		yield return (_firstLeg, DataType.Level1);

		if (_secondLeg != null)
		yield return (_secondLeg, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_combination = null;
		_firstLeg = null;
		_secondLeg = null;

		_firstBid = null;
		_firstAsk = null;
		_secondBid = null;
		_secondAsk = null;
		_syntheticBid = null;
		_syntheticAsk = null;
		_lastLoggedBid = null;
		_lastLoggedAsk = null;

		_isPlacingOrders = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureCombinationInitialized(true);

		if (_firstLeg == null || _secondLeg == null)
		return;

		SubscribeLevel1(_firstLeg)
		.Bind(message => ProcessLevel1(_firstLeg, message))
		.Start();

		SubscribeLevel1(_secondLeg)
		.Bind(message => ProcessLevel1(_secondLeg, message))
		.Start();

		LogInfo($"Symbol synthesizer initialized with {_combination?.SyntheticSymbol} = {_combination?.FirstLeg} {_combination?.SecondLeg} ({(_combination?.IsProduct == true ? "product" : "ratio")}).");
	}

	private void EnsureCombinationInitialized(bool throwOnError)
	{
		if (_combination != null)
		return;

		var index = CombinationIndex;

		if (index < 0 || index >= _predefinedCombinations.Length)
		{
		var message = $"Combination index {index} is out of range (0..{_predefinedCombinations.Length - 1}).";
		if (throwOnError)
		throw new InvalidOperationException(message);

		LogWarning(message);
		return;
		}

		_combination = _predefinedCombinations[index];

		_firstLeg = this.GetSecurity(_combination.FirstLeg);
		_secondLeg = this.GetSecurity(_combination.SecondLeg);

		if (_firstLeg == null || _secondLeg == null)
		{
		var missing = _firstLeg == null ? _combination.FirstLeg : _combination.SecondLeg;
		var message = $"Security '{missing}' could not be resolved.";
		_combination = null;
		_firstLeg = null;
		_secondLeg = null;

		if (throwOnError)
		throw new InvalidOperationException(message);

		LogWarning(message);
		return;
		}

		if (Security == null)
		{
		var synthetic = this.GetSecurity(_combination.SyntheticSymbol);
		Security = synthetic ?? _firstLeg;
		}
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		if (security == _firstLeg)
		{
		UpdateBidAsk(ref _firstBid, ref _firstAsk, message);
		}
		else if (security == _secondLeg)
		{
		UpdateBidAsk(ref _secondBid, ref _secondAsk, message);
		}
		else
		{
		return;
		}

		UpdateSyntheticQuotes();
	}

	private static void UpdateBidAsk(ref decimal? bidStorage, ref decimal? askStorage, Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BidPrice);
		if (bid is decimal bidPrice && bidPrice > 0m)
		bidStorage = bidPrice;

		var ask = message.TryGetDecimal(Level1Fields.AskPrice);
		if (ask is decimal askPrice && askPrice > 0m)
		askStorage = askPrice;
	}

	private void UpdateSyntheticQuotes()
	{
		if (_combination == null)
		return;

		if (_firstBid is not decimal firstBid || _firstAsk is not decimal firstAsk ||
			_secondBid is not decimal secondBid || _secondAsk is not decimal secondAsk)
		{
		return;
		}

		decimal newBid;
		decimal newAsk;

		if (_combination.IsProduct)
		{
		newBid = firstBid * secondBid;
		newAsk = firstAsk * secondAsk;
		}
		else
		{
		if (firstBid <= 0m || firstAsk <= 0m)
		return;

		newBid = secondBid / firstBid;
		newAsk = secondAsk / firstAsk;
		}

		if (_syntheticBid != newBid || _syntheticAsk != newAsk)
		{
		_syntheticBid = newBid;
		_syntheticAsk = newAsk;

		if (_lastLoggedBid != newBid || _lastLoggedAsk != newAsk)
		{
		_lastLoggedBid = newBid;
		_lastLoggedAsk = newAsk;
		LogInfo($"Synthetic {_combination.SyntheticSymbol} quote updated. Bid={newBid}, Ask={newAsk}.");
		}

		TryExecuteManualAction();
		}
	}

	private void TryExecuteManualAction()
	{
		if (_isPlacingOrders)
		return;

		var action = TradeAction;

		if (action == SyntheticTradeAction.None)
		return;

	_isPlacingOrders = true;

		try
		{
		ExecuteSyntheticTrade(action);
		TradeAction = SyntheticTradeAction.None;
		}
		catch (Exception ex)
		{
		LogError($"Failed to execute synthetic {action}. {ex.Message}");
		TradeAction = SyntheticTradeAction.None;
		}
		finally
		{
		_isPlacingOrders = false;
		}
	}

	private void ExecuteSyntheticTrade(SyntheticTradeAction action)
	{
		if (_combination == null || _firstLeg == null || _secondLeg == null)
		throw new InvalidOperationException("Strategy legs are not initialized.");

		if (OrderVolume <= 0m)
		throw new InvalidOperationException("OrderVolume must be greater than zero.");

		if (_firstBid is not decimal firstBid || _firstAsk is not decimal firstAsk ||
			_secondBid is not decimal secondBid || _secondAsk is not decimal secondAsk)
		{
		throw new InvalidOperationException("Leg quotes are not ready yet.");
		}

		if (_syntheticBid is not decimal synthBid || _syntheticAsk is not decimal synthAsk)
		throw new InvalidOperationException("Synthetic quotes are not ready yet.");

		var firstSide = _combination.IsProduct
		? (action == SyntheticTradeAction.Buy ? Sides.Buy : Sides.Sell)
		: (action == SyntheticTradeAction.Buy ? Sides.Sell : Sides.Buy);

		var secondSide = action == SyntheticTradeAction.Buy ? Sides.Buy : Sides.Sell;

		var firstPrice = firstSide == Sides.Buy ? firstAsk : firstBid;
		var secondPrice = secondSide == Sides.Buy ? secondAsk : secondBid;

		if (firstPrice <= 0m || secondPrice <= 0m)
		throw new InvalidOperationException("Reference prices are not positive.");

		var firstVolume = NormalizeVolume(OrderVolume, _firstLeg);

		var firstTickValue = _firstLeg.StepPrice ?? 0m;
		var secondTickValue = _secondLeg.StepPrice ?? 0m;
		var firstPoint = _firstLeg.PriceStep ?? 0m;
		var secondPoint = _secondLeg.PriceStep ?? 0m;

		if (firstTickValue <= 0m || secondTickValue <= 0m || firstPoint <= 0m || secondPoint <= 0m)
		{
		throw new InvalidOperationException("Tick value or price step metadata is missing.");
		}

		var syntheticPrice = action == SyntheticTradeAction.Buy ? synthAsk : synthBid;

		if (syntheticPrice <= 0m)
		throw new InvalidOperationException("Synthetic price must be positive.");

		var rawSecondVolume = firstVolume * syntheticPrice / firstTickValue / secondTickValue * (secondPoint / firstPoint);
		var secondVolume = NormalizeVolume(rawSecondVolume, _secondLeg);

		PlaceOrder(_firstLeg, firstSide, firstVolume, firstPrice);
		PlaceOrder(_secondLeg, secondSide, secondVolume, secondPrice);

		LogInfo($"Placed synthetic {action} orders: {firstSide} {firstVolume} {_firstLeg.Id}, {secondSide} {secondVolume} {_secondLeg.Id}.");
	}

	private decimal NormalizeVolume(decimal volume, Security security)
	{
		var minVolume = security.MinVolume ?? 0m;
		var maxVolume = security.MaxVolume;
		var step = security.VolumeStep ?? 0.01m;

		if (step > 0m)
		{
		var steps = Math.Round(volume / step, 0, MidpointRounding.AwayFromZero);
		volume = steps * step;
		}

		if (volume < minVolume)
		volume = minVolume > 0m ? minVolume : step;

		if (maxVolume is decimal max && volume > max)
		volume = max;

		return volume;
	}

	private void PlaceOrder(Security security, Sides side, decimal volume, decimal referencePrice)
	{
		var price = referencePrice;
		var priceStep = security.PriceStep ?? 0m;

		if (priceStep > 0m && Slippage > 0m)
		{
		var offset = Slippage * priceStep;
		price = side == Sides.Buy ? referencePrice + offset : referencePrice - offset;
		}

		price = security.ShrinkPrice(price);

		if (side == Sides.Buy)
		{
		BuyLimit(volume, price, security);
		}
		else
		{
		SellLimit(volume, price, security);
		}
	}
}

/// <summary>
/// Manual actions exposed by <see cref="SymbolSynthesizerStrategy"/>.
/// </summary>
public enum SyntheticTradeAction
{
	/// <summary>
	/// No action requested.
	/// </summary>
	None,

	/// <summary>
	/// Open the synthetic position using buy logic.
	/// </summary>
	Buy,

	/// <summary>
	/// Open the synthetic position using sell logic.
	/// </summary>
	Sell
}

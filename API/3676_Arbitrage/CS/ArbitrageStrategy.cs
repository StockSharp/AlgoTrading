namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Triangular arbitrage strategy converted from the MetaTrader "Arbitrage" expert advisor.
/// The strategy looks for price discrepancies between EURUSD, GBPUSD, and EURGBP and opens
/// hedged baskets whenever the synthetic rate diverges enough to cover trading costs.
/// </summary>
public class ArbitrageStrategy : Strategy
{
	private readonly StrategyParam<Security> _firstLegParam;
	private readonly StrategyParam<Security> _secondLegParam;
	private readonly StrategyParam<Security> _crossPairParam;
	private readonly StrategyParam<decimal> _lotSizePerThousandParam;
	private readonly StrategyParam<decimal> _commissionPerLotParam;
	private readonly StrategyParam<bool> _logMaxDifferenceParam;

	private decimal? _firstBid;
	private decimal? _firstAsk;
	private decimal? _secondBid;
	private decimal? _secondAsk;
	private decimal? _crossBid;
	private decimal? _crossAsk;

	private decimal _largestDifference;

	/// <summary>
	/// Primary leg traded on the EURUSD side of the arbitrage triangle.
	/// </summary>
	public Security FirstLeg
	{
		get => _firstLegParam.Value;
		set => _firstLegParam.Value = value;
	}

	/// <summary>
	/// Secondary leg traded on the GBPUSD side of the arbitrage triangle.
	/// </summary>
	public Security SecondLeg
	{
		get => _secondLegParam.Value;
		set => _secondLegParam.Value = value;
	}

	/// <summary>
	/// Cross pair connecting EUR and GBP (EURGBP by default).
	/// </summary>
	public Security CrossPair
	{
		get => _crossPairParam.Value;
		set => _crossPairParam.Value = value;
	}

	/// <summary>
	/// Lot size multiplier expressed per thousand units of account currency.
	/// </summary>
	public decimal LotSizePerThousand
	{
		get => _lotSizePerThousandParam.Value;
		set => _lotSizePerThousandParam.Value = value;
	}

	/// <summary>
	/// Total commission charged per round trip lot across the basket.
	/// </summary>
	public decimal CommissionPerLot
	{
		get => _commissionPerLotParam.Value;
		set => _commissionPerLotParam.Value = value;
	}

	/// <summary>
	/// Enables logging of the largest observed synthetic price discrepancy.
	/// </summary>
	public bool LogMaxDifference
	{
		get => _logMaxDifferenceParam.Value;
		set => _logMaxDifferenceParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ArbitrageStrategy"/> class.
	/// </summary>
	public ArbitrageStrategy()
	{
		_firstLegParam = Param<Security>(nameof(FirstLeg))
		.SetDisplay("First Leg", "Primary leg (EURUSD) used in the synthetic rate", "Connectivity")
		.SetRequired();

		_secondLegParam = Param<Security>(nameof(SecondLeg))
		.SetDisplay("Second Leg", "Secondary leg (GBPUSD) used in the synthetic rate", "Connectivity")
		.SetRequired();

		_crossPairParam = Param<Security>(nameof(CrossPair))
		.SetDisplay("Cross Pair", "Cross currency pair (EURGBP) compared with the synthetic rate", "Connectivity")
		.SetRequired();

		_lotSizePerThousandParam = Param(nameof(LotSizePerThousand), 0.01m)
		.SetDisplay("Lots per $1k", "Position size multiplier per thousand units of account balance", "Risk Management")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_commissionPerLotParam = Param(nameof(CommissionPerLot), 7m)
		.SetDisplay("Commission per Lot", "Total commission charged for all three legs", "Costs")
		.SetGreaterThanZero();

		_logMaxDifferenceParam = Param(nameof(LogMaxDifference), false)
		.SetDisplay("Log Max Difference", "Write the largest observed arbitrage gap to the log", "Diagnostics");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(FirstLeg, DataType.Level1),
		(SecondLeg, DataType.Level1),
		(CrossPair, DataType.Level1)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstBid = null;
		_firstAsk = null;
		_secondBid = null;
		_secondAsk = null;
		_crossBid = null;
		_crossAsk = null;
		_largestDifference = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (FirstLeg == null)
		throw new InvalidOperationException("First leg security is not specified.");

		if (SecondLeg == null)
		throw new InvalidOperationException("Second leg security is not specified.");

		if (CrossPair == null)
		throw new InvalidOperationException("Cross pair security is not specified.");

		SubscribeLevel1(FirstLeg)
		.Bind(OnFirstLegLevel1)
		.Start();

		SubscribeLevel1(SecondLeg)
		.Bind(OnSecondLegLevel1)
		.Start();

		SubscribeLevel1(CrossPair)
		.Bind(OnCrossPairLevel1)
		.Start();
	}

	private void OnFirstLegLevel1(Level1ChangeMessage message)
	{
		_firstBid = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _firstBid;
		_firstAsk = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _firstAsk;

		EvaluateOpportunity();
	}

	private void OnSecondLegLevel1(Level1ChangeMessage message)
	{
		_secondBid = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _secondBid;
		_secondAsk = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _secondAsk;

		EvaluateOpportunity();
	}

	private void OnCrossPairLevel1(Level1ChangeMessage message)
	{
		_crossBid = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _crossBid;
		_crossAsk = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _crossAsk;

		EvaluateOpportunity();
	}

	private void EvaluateOpportunity()
	{
		if (!_firstAsk.HasValue || !_firstBid.HasValue)
		return;

		if (!_secondAsk.HasValue || !_secondBid.HasValue)
		return;

		if (!_crossAsk.HasValue || !_crossBid.HasValue)
		return;

		var eurUsdAsk = _firstAsk.Value;
		var eurUsdBid = _firstBid.Value;
		var gbpUsdAsk = _secondAsk.Value;
		var gbpUsdBid = _secondBid.Value;
		var eurGbpAsk = _crossAsk.Value;
		var eurGbpBid = _crossBid.Value;

		if (eurUsdAsk <= 0m || eurUsdBid <= 0m || gbpUsdAsk <= 0m || gbpUsdBid <= 0m || eurGbpAsk <= 0m || eurGbpBid <= 0m)
		return;

		var syntheticSell = eurUsdAsk / gbpUsdBid;
		var syntheticBuy = eurUsdBid / gbpUsdAsk;

		var differenceForSell = syntheticSell - eurGbpAsk;
		var differenceForBuy = syntheticBuy - eurGbpBid;

		var spreadsCost = (eurUsdAsk - eurUsdBid) + (gbpUsdAsk - gbpUsdBid) + (eurGbpAsk - eurGbpBid);
		var point = GetPointSize();
		var digits = GetDigits();
		var commissionComponent = 3m * CommissionPerLot * point;
		var roundedCost = RoundToDigits(commissionComponent + spreadsCost, digits);
		var positiveThreshold = point + roundedCost;
		var negativeThreshold = -point - roundedCost;

		var absoluteDifference = Math.Abs(differenceForSell);
		if (LogMaxDifference && absoluteDifference > _largestDifference)
		{
			_largestDifference = absoluteDifference;
			LogInfo($"Largest synthetic gap {absoluteDifference:F5}. Required edge {positiveThreshold:F5}.");
		}

		if (differenceForSell > positiveThreshold)
		{
			CloseNegativeSide();
			if (!HasOpenPositions())
			{
				ExecuteBasket(isBuyingCross: true);
			}
		}
		else if (differenceForBuy < negativeThreshold)
		{
			ClosePositiveSide();
			if (!HasOpenPositions())
			{
				ExecuteBasket(isBuyingCross: false);
			}
		}
	}

	private void ExecuteBasket(bool isBuyingCross)
	{
		var referenceSecurity = CrossPair ?? Security;
		if (referenceSecurity == null)
		return;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m)
		return;

		var rawVolume = portfolioValue / 1000m * LotSizePerThousand;
		var volume = RoundToVolume(rawVolume, referenceSecurity);

		if (referenceSecurity.MaxVolume is decimal maxVolume && maxVolume > 0m)
		volume = Math.Min(volume, maxVolume);

		if (volume <= 0m)
		return;

		if (isBuyingCross)
		{
			SellMarket(volume, FirstLeg);
			BuyMarket(volume, SecondLeg);
			BuyMarket(volume, CrossPair);
		}
		else
		{
			BuyMarket(volume, FirstLeg);
			SellMarket(volume, SecondLeg);
			SellMarket(volume, CrossPair);
		}
	}

	private void ClosePositiveSide()
	{
		CloseLong(SecondLeg);
		CloseLong(CrossPair);
		CloseShort(FirstLeg);
	}

	private void CloseNegativeSide()
	{
		CloseShort(SecondLeg);
		CloseShort(CrossPair);
		CloseLong(FirstLeg);
	}

	private void CloseLong(Security security)
	{
		if (security == null)
		return;

		var volume = GetPositionValue(security, Portfolio) ?? 0m;
		if (volume > 0m)
		SellMarket(volume, security);
	}

	private void CloseShort(Security security)
	{
		if (security == null)
		return;

		var volume = GetPositionValue(security, Portfolio) ?? 0m;
		if (volume < 0m)
		BuyMarket(Math.Abs(volume), security);
	}

	private bool HasOpenPositions()
	{
		var first = GetPositionValue(FirstLeg, Portfolio) ?? 0m;
		var second = GetPositionValue(SecondLeg, Portfolio) ?? 0m;
		var cross = GetPositionValue(CrossPair, Portfolio) ?? 0m;

		return first != 0m || second != 0m || cross != 0m;
	}

	private decimal GetPointSize()
	{
		var decimals = GetDigits();
		if (decimals <= 0)
		{
			var step = CrossPair?.PriceStep ?? Security?.PriceStep;
			return step ?? 0.0001m;
		}

		var point = 1m;
		for (var i = 0; i < decimals; i++)
		{
			point /= 10m;
		}

		return point;
	}

	private int GetDigits()
	{
		return CrossPair?.Decimals ?? Security?.Decimals ?? 5;
	}

	private static decimal RoundToDigits(decimal value, int digits)
	{
		return Math.Round(value, digits, MidpointRounding.AwayFromZero);
	}

	private static decimal RoundToVolume(decimal value, Security security, bool down = false)
	{
		if (security == null)
		return value;

		var step = security.VolumeStep ?? 0m;
		if (step <= 0m)
		return value;

		var ratio = value / step;
		decimal rounded;

		if (down)
		{
			rounded = Math.Floor(ratio);
		}
		else
		{
			rounded = Math.Round(ratio, 0, MidpointRounding.AwayFromZero);
		}

		return rounded * step;
	}
}


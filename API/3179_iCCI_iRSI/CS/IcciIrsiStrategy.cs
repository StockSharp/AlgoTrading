namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// CCI and RSI threshold strategy converted from the MetaTrader expert advisor "iCCI iRSI".
/// Buys oversold conditions when both oscillators drop below their lower bands and sells when they climb above the upper bands.
/// Optional stop-loss, take-profit, and trailing-stop distances are expressed in pips to match the original inputs.
/// </summary>
public class IcciIrsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciUpperLevel;
	private readonly StrategyParam<decimal> _cciLowerLevel;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private CommodityChannelIndex _cci = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Averaging period of the CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold that defines overbought conditions.
	/// </summary>
	public decimal CciUpperLevel
	{
		get => _cciUpperLevel.Value;
		set => _cciUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold that defines oversold conditions.
	/// </summary>
	public decimal CciLowerLevel
	{
		get => _cciLowerLevel.Value;
		set => _cciLowerLevel.Value = value;
	}

	/// <summary>
	/// Period of the RSI oscillator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI band that triggers sell decisions.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI band that triggers buy decisions.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Whether to reverse the direction of the signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Volume of each market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price improvement in pips required before the trailing stop is advanced.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IcciIrsiStrategy"/> class.
	/// </summary>
	public IcciIrsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for the indicators", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Number of bars for the CCI smoothing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_cciUpperLevel = Param(nameof(CciUpperLevel), 80m)
		.SetDisplay("CCI Upper", "Overbought threshold for CCI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60m, 140m, 10m);

		_cciLowerLevel = Param(nameof(CciLowerLevel), -80m)
		.SetDisplay("CCI Lower", "Oversold threshold for CCI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(-140m, -40m, 10m);

		_rsiPeriod = Param(nameof(RsiPeriod), 42)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars for the RSI calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 5);

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60m)
		.SetDisplay("RSI Upper", "Overbought threshold for RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(55m, 80m, 5m);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 30m)
		.SetDisplay("RSI Lower", "Oversold threshold for RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20m, 45m, 5m);

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse Signals", "Flip entry direction when enabled", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume submitted with each market order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetNonNegative()
		.SetDisplay("Stop Loss", "Protective stop-loss distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 20m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140m)
		.SetNonNegative()
		.SetDisplay("Take Profit", "Profit target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(40m, 300m, 20m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNonNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 50m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNonNegative()
		.SetDisplay("Trailing Step", "Minimum progress before updating the trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 20m, 2m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, _rsi, ProcessCandle)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}

		var cciArea = CreateChartArea("CCI");
		if (cciArea != null)
		DrawIndicator(cciArea, _cci);

		var rsiArea = CreateChartArea("RSI");
		if (rsiArea != null)
		DrawIndicator(rsiArea, _rsi);
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageActivePosition(candle);

		// Evaluate new signals only after managing the existing position.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_cci.IsFormed || !_rsi.IsFormed)
		return;

		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var shouldBuy = false;
		var shouldSell = false;

		if (!ReverseSignals)
		{
			shouldBuy = cciValue <= CciLowerLevel && rsiValue <= RsiLowerLevel;
			shouldSell = cciValue >= CciUpperLevel && rsiValue >= RsiUpperLevel;
		}
		else
		{
			shouldBuy = cciValue >= CciUpperLevel && rsiValue >= RsiUpperLevel;
			shouldSell = cciValue <= CciLowerLevel && rsiValue <= RsiLowerLevel;
		}

		if (shouldBuy)
		{
			var requiredVolume = volume + Math.Max(0m, -Position);
			if (requiredVolume > 0m)
			{
				BuyMarket(requiredVolume);
				ResetShortState();
				InitializeLongState(candle.ClosePrice);
			}
		}
		else if (shouldSell)
		{
			var requiredVolume = volume + Math.Max(0m, Position);
			if (requiredVolume > 0m)
			{
				SellMarket(requiredVolume);
				ResetLongState();
				InitializeShortState(candle.ClosePrice);
			}
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		// Replicate the MetaTrader exit handling by checking protective targets before new entries.
		if (Position > 0 && _longEntryPrice is decimal)
		{
			var price = candle.ClosePrice;

			if (_longTakePrice is decimal take && price >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}

			if (_longStopPrice is decimal stop && price <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}

			UpdateLongTrailing(price);
		}
		else if (Position < 0 && _shortEntryPrice is decimal)
		{
			var price = candle.ClosePrice;

			if (_shortTakePrice is decimal take && price <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			if (_shortStopPrice is decimal stop && price >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			UpdateShortTrailing(price);
		}
		else
		{
			if (Position <= 0)
			ResetLongState();

			if (Position >= 0)
			ResetShortState();
		}
	}

	private void InitializeLongState(decimal entryPrice)
	{
		// Store entry context and translate pip-based offsets to price levels.
		_longEntryPrice = entryPrice;
		_longTakePrice = CreateTargetPrice(entryPrice, TakeProfitPips, true);
		_longStopPrice = CreateTargetPrice(entryPrice, StopLossPips, false);
	}

	private void InitializeShortState(decimal entryPrice)
	{
		// Store short entry context and calculate the matching protective levels.
		_shortEntryPrice = entryPrice;
		_shortTakePrice = CreateTargetPrice(entryPrice, TakeProfitPips, false);
		_shortStopPrice = CreateTargetPrice(entryPrice, StopLossPips, true);
	}

	private void UpdateLongTrailing(decimal price)
	{
		// Advance the trailing stop only after sufficient favourable movement.
		var trailDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailDistance <= 0m || _longEntryPrice is not decimal entry)
		return;

		var trailStep = ConvertPipsToPrice(TrailingStepPips);
		if (price - entry <= trailDistance + trailStep)
		return;

		var candidate = price - trailDistance;
		if (_longStopPrice is null || candidate - _longStopPrice.Value >= trailStep)
		_longStopPrice = candidate;
	}

	private void UpdateShortTrailing(decimal price)
	{
		// Mirror the long trailing logic for short positions.
		var trailDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailDistance <= 0m || _shortEntryPrice is not decimal entry)
		return;

		var trailStep = ConvertPipsToPrice(TrailingStepPips);
		if (entry - price <= trailDistance + trailStep)
		return;

		var candidate = price + trailDistance;
		if (_shortStopPrice is null || _shortStopPrice.Value - candidate >= trailStep)
		_shortStopPrice = candidate;
	}

	private decimal? CreateTargetPrice(decimal entryPrice, decimal distanceInPips, bool add)
	{
		var offset = ConvertPipsToPrice(distanceInPips);
		if (offset <= 0m)
		return null;

		return add ? entryPrice + offset : entryPrice - offset;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		// Convert MetaTrader-style pip distances into actual price increments.
		if (pips <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep.HasValue ? (decimal)security.PriceStep.Value : 0m;
		if (step <= 0m)
		return 0m;

		var decimals = security.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return step * multiplier * pips;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}
}

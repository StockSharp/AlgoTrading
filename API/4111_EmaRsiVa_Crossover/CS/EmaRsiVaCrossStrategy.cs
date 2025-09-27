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
/// Port of the EA_MARSI expert advisor.
/// Uses two EMA_RSI_VA indicators and trades on their crossovers.
/// </summary>
public class EmaRsiVaCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useBalanceMultiplier;
	private readonly StrategyParam<decimal> _maxDrawdown;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<AppliedPrice> _slowAppliedPrice;
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<AppliedPrice> _fastAppliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private EmaRsiVolatilityAdaptiveIndicator _slowIndicator = default!;
	private EmaRsiVolatilityAdaptiveIndicator _fastIndicator = default!;
	private decimal? _previousSlow;
	private decimal? _previousFast;


	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables balance based volume multiplier.
	/// </summary>
	public bool UseBalanceMultiplier
	{
		get => _useBalanceMultiplier.Value;
		set => _useBalanceMultiplier.Value = value;
	}

	/// <summary>
	/// Drawdown threshold used by the volume multiplier.
	/// </summary>
	public decimal MaxDrawdown
	{
		get => _maxDrawdown.Value;
		set => _maxDrawdown.Value = value;
	}

	/// <summary>
	/// RSI period for the slow EMA_RSI_VA indicator.
	/// </summary>
	public int SlowRsiPeriod
	{
		get => _slowRsiPeriod.Value;
		set => _slowRsiPeriod.Value = value;
	}

	/// <summary>
	/// EMA period for the the slow EMA_RSI_VA indicator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mode for the slow EMA_RSI_VA indicator.
	/// </summary>
	public AppliedPrice SlowAppliedPrice
	{
		get => _slowAppliedPrice.Value;
		set => _slowAppliedPrice.Value = value;
	}

	/// <summary>
	/// RSI period for the fast EMA_RSI_VA indicator.
	/// </summary>
	public int FastRsiPeriod
	{
		get => _fastRsiPeriod.Value;
		set => _fastRsiPeriod.Value = value;
	}

	/// <summary>
	/// EMA period for the fast EMA_RSI_VA indicator.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mode for the fast EMA_RSI_VA indicator.
	/// </summary>
	public AppliedPrice FastAppliedPrice
	{
		get => _fastAppliedPrice.Value;
		set => _fastAppliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaRsiVaCrossStrategy"/>.
	/// </summary>
	public EmaRsiVaCrossStrategy()
	{

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Optional take-profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Optional stop-loss distance in points", "Risk");

		_useBalanceMultiplier = Param(nameof(UseBalanceMultiplier), false)
		.SetDisplay("Use Balance Multiplier", "Scale volume proportionally to account equity", "Money Management");

		_maxDrawdown = Param(nameof(MaxDrawdown), 10000m)
		.SetGreaterThanZero()
		.SetDisplay("Max Drawdown", "Equity threshold used by the multiplier", "Money Management");

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 310)
		.SetGreaterThanZero()
		.SetDisplay("Slow RSI Period", "Slow EMA_RSI_VA RSI length", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 40)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Period", "Slow EMA_RSI_VA smoothing length", "Indicators");

		_slowAppliedPrice = Param(nameof(SlowAppliedPrice), AppliedPrice.Close)
		.SetDisplay("Slow Applied Price", "Price input for the slow indicator", "Indicators");

		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("Fast RSI Period", "Fast EMA_RSI_VA RSI length", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Period", "Fast EMA_RSI_VA smoothing length", "Indicators");

		_fastAppliedPrice = Param(nameof(FastAppliedPrice), AppliedPrice.Close)
		.SetDisplay("Fast Applied Price", "Price input for the fast indicator", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for signals", "General");
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

		_previousSlow = null;
		_previousFast = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Build both EMA_RSI_VA indicators.
		_slowIndicator = new EmaRsiVolatilityAdaptiveIndicator
		{
			RsiPeriod = SlowRsiPeriod,
			EmaPeriod = SlowEmaPeriod,
			AppliedPrice = SlowAppliedPrice,
		};

		_fastIndicator = new EmaRsiVolatilityAdaptiveIndicator
		{
			RsiPeriod = FastRsiPeriod,
			EmaPeriod = FastEmaPeriod,
			AppliedPrice = FastAppliedPrice,
		};

		var subscription = SubscribeCandles(CandleType);

		// Configure protective orders according to requested distances.
		var priceStep = Security?.PriceStep ?? 0m;
		var point = priceStep > 0m ? priceStep : 1m;
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * point : 0m;
		var takeDistance = TakeProfitPoints > 0m ? TakeProfitPoints * point : 0m;

		if (stopDistance > 0m || takeDistance > 0m)
		{
			StartProtection(
				takeProfit: takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : null,
				stopLoss: stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : null,
				useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		subscription
			.Bind(_slowIndicator, _fastIndicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue)
	{
		// Only completed candles participate in the logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until both indicators are formed.
		if (!_slowIndicator.IsFormed || !_fastIndicator.IsFormed)
			return;

		// Ensure trading is allowed and connection is healthy.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousSlow is null || _previousFast is null)
		{
			_previousSlow = slowValue;
			_previousFast = fastValue;
			return;
		}

		var prevSlow = _previousSlow.Value;
		var prevFast = _previousFast.Value;

		var sellSignal = prevSlow < prevFast && slowValue >= fastValue;
		var buySignal = prevSlow > prevFast && slowValue <= fastValue;

		if (sellSignal)
		{
			ExecuteSignal(Sides.Sell);
		}
		else if (buySignal)
		{
			ExecuteSignal(Sides.Buy);
		}

		_previousSlow = slowValue;
		_previousFast = fastValue;
	}

	private void ExecuteSignal(Sides direction)
	{
		if (direction == Sides.Sell && Position < 0m)
			return;

		if (direction == Sides.Buy && Position > 0m)
			return;

		var baseVolume = CalculateBaseVolume();
		if (baseVolume <= 0m)
			return;

		var totalVolume = direction == Sides.Buy
			? baseVolume + (Position < 0m ? -Position : 0m)
			: baseVolume + (Position > 0m ? Position : 0m);

		var security = Security;
		if (security?.MaxVolume is decimal maxVolume && maxVolume > 0m && totalVolume > maxVolume)
		{
			// Respect exchange limits by splitting the reversal into two orders.
			var closeVolume = direction == Sides.Buy ? -Position : Position;
			closeVolume = NormalizeVolume(Math.Abs(closeVolume));
			if (closeVolume > 0m)
			{
				if (direction == Sides.Buy)
					BuyMarket(closeVolume);
				else
					SellMarket(closeVolume);
			}

			totalVolume = baseVolume;
		}

		totalVolume = NormalizeVolume(totalVolume);
		if (totalVolume <= 0m)
			return;

		if (direction == Sides.Buy)
			BuyMarket(totalVolume);
		else
			SellMarket(totalVolume);
	}

	private decimal CalculateBaseVolume()
	{
		var volume = Volume;

		if (UseBalanceMultiplier && MaxDrawdown > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (equity > 0m)
			{
				var scaled = Volume * equity / MaxDrawdown;
				if (scaled > 0m)
					volume = scaled;
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		if (security.MinVolume is decimal minVolume && minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (security.MaxVolume is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return volume;
	}
}

/// <summary>
/// Volatility adaptive EMA implementation driven by RSI displacement.
/// </summary>
public class EmaRsiVolatilityAdaptiveIndicator : BaseIndicator<decimal>
{
	private readonly RelativeStrengthIndex _rsi = new();
	private decimal? _previousValue;

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod { get; set; } = 14;

	/// <summary>
	/// Base EMA length multiplied by the adaptive factor.
	/// </summary>
	public int EmaPeriod { get; set; } = 14;

	/// <summary>
	/// Price source applied to incoming candles.
	/// </summary>
	public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		if (_rsi.Length != RsiPeriod)
		{
			_rsi.Length = RsiPeriod;
			_rsi.Reset();
			_previousValue = null;
		}

		var price = GetAppliedPrice(candle, AppliedPrice);
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, price, candle.OpenTime));
		if (!rsiValue.IsFinal)
		{
			IsFormed = false;
			return new DecimalIndicatorValue(this, default, input.Time);
		}

		var rsi = rsiValue.ToDecimal();
		var rsVolatility = Math.Abs(rsi - 50m) + 1m;
		var rsiLength = Math.Max(1, RsiPeriod);
		var multiplier = (5m + 100m / rsiLength) / (0.06m + 0.92m * rsVolatility + 0.02m * rsVolatility * rsVolatility);
		var dynamicPeriod = multiplier * Math.Max(1m, EmaPeriod);
		if (dynamicPeriod < 1m)
			dynamicPeriod = 1m;

		var smoothingFactor = 2m / (dynamicPeriod + 1m);

		decimal current;
		if (_previousValue is null)
		{
			// Initialize the recursive EMA with the first available price.
			current = price;
			_previousValue = current;
			IsFormed = false;
			return new DecimalIndicatorValue(this, current, input.Time);
		}

		current = price * smoothingFactor + _previousValue.Value * (1m - smoothingFactor);
		_previousValue = current;
		IsFormed = true;

		return new DecimalIndicatorValue(this, current, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_rsi.Reset();
		_previousValue = null;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
		if (candle.ClosePrice < candle.OpenPrice)
			sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			sum = (sum + candle.HighPrice) / 2m;
		else
			sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}
}

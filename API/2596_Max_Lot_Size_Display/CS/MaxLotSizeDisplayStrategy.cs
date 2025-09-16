using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the EXP_MAX_LOT indicator from MetaTrader.
/// Calculates the maximum order volume that current free funds allow for a chosen direction.
/// Logs the result and assigns it to <see cref="Strategy.Volume"/> for further use.
/// </summary>
public class MaxLotSizeDisplayStrategy : Strategy
{
	private readonly StrategyParam<Sides> _tradeDirection;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<string> _labelPrefix;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastComputedVolume;

	/// <summary>
	/// Direction (long or short) used when estimating the maximum volume.
	/// </summary>
	public Sides TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Fraction of free capital that can be allocated to a position.
	/// Value of 1.0 means the whole available margin is used.
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Prefix added to informational log messages.
	/// </summary>
	public string LabelPrefix
	{
		get => _labelPrefix.Value;
		set => _labelPrefix.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger recalculation of the maximum volume.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaxLotSizeDisplayStrategy"/> class.
	/// </summary>
	public MaxLotSizeDisplayStrategy()
	{
		_tradeDirection = Param(nameof(TradeDirection), Sides.Buy)
			.SetDisplay("Trade Direction", "Direction to evaluate for maximum volume", "General");

		_riskFraction = Param(nameof(RiskFraction), 1m)
			.SetRange(0m, 10m)
			.SetDisplay("Risk Fraction", "Fraction of free funds to allocate", "Risk Management");

		_labelPrefix = Param(nameof(LabelPrefix), "MAX_LOT_Label_")
			.SetDisplay("Label Prefix", "Text prefix used in log messages", "Visualization");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles that trigger updates", "General");
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
		_lastComputedVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxVolume = CalculateMaxVolume(candle.ClosePrice);

		if (maxVolume > 0m)
			Volume = maxVolume;

		if (_lastComputedVolume is decimal previous && previous == maxVolume)
			return;

		_lastComputedVolume = maxVolume;

		if (maxVolume > 0m)
		{
			LogInfo($"{LabelPrefix}Maximum volume for {TradeDirection} = {maxVolume:0.####}");
		}
		else
		{
			LogWarn($"{LabelPrefix}Not enough free funds to open {TradeDirection} position.");
		}
	}

	private decimal CalculateMaxVolume(decimal price)
	{
		var security = Security;
		var portfolio = Portfolio;

		if (security == null || portfolio == null)
			return 0m;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var freeFunds = equity * RiskFraction;
		if (freeFunds <= 0m)
			return 0m;

		var marginPerVolume = GetMarginPerVolume(security, price);
		if (marginPerVolume <= 0m)
			return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		var minVolume = security.MinVolume ?? volumeStep;
		var maxVolumeLimit = security.MaxVolume ?? decimal.MaxValue;

		var rawVolume = freeFunds / marginPerVolume;
		if (rawVolume <= 0m)
			return 0m;

		var steps = decimal.Floor(rawVolume / volumeStep);
		if (steps <= 0m)
			return 0m;

		var normalizedVolume = steps * volumeStep;

		if (normalizedVolume < minVolume)
			return 0m;

		if (normalizedVolume > maxVolumeLimit)
			normalizedVolume = maxVolumeLimit;

		return normalizedVolume;
	}

	private decimal GetMarginPerVolume(Security security, decimal price)
	{
		var margin = TradeDirection == Sides.Buy ? security.MarginBuy : security.MarginSell;
		if (margin is decimal directMargin && directMargin > 0m)
			return directMargin;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		if (price <= 0m)
			return 0m;

		var estimatedMargin = price * volumeStep;
		return estimatedMargin > 0m ? estimatedMargin : 0m;
	}
}

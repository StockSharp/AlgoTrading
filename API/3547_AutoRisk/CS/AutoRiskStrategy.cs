using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the AutoRisk expert advisor volume calculation.
/// Calculates the recommended order volume based on daily ATR and account metrics.
/// </summary>
public class AutoRiskStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<AutoRiskCalculationMode> _calculationMode;
	private readonly StrategyParam<bool> _roundUp;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastAtrValue;
	private bool _hasAtr;
	private decimal _recommendedVolume;

	/// <summary>
	/// Initializes strategy parameters with defaults from the MQL version.
	/// </summary>
	public AutoRiskStrategy()
	{
		_riskFactor = Param(nameof(RiskFactor), 2m)
			.SetDisplay("Risk % on daily ATR")
			.SetCanOptimize(true);

		_calculationMode = Param(nameof(CalculationMode), AutoRiskCalculationMode.Balance)
			.SetDisplay("Account metric used in the lot size formula");

		_roundUp = Param(nameof(RoundUp), true)
			.SetDisplay("Round the calculated volume to the nearest step");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromDays(1)))
			.SetDisplay("Candle type for ATR calculation");
	}

	/// <summary>
	/// Risk percentage applied to the ATR based position sizing.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Determines which portfolio metric is used for the calculation.
	/// </summary>
	public AutoRiskCalculationMode CalculationMode
	{
		get => _calculationMode.Value;
		set => _calculationMode.Value = value;
	}

	/// <summary>
	/// Enables rounding to the nearest volume step instead of flooring it.
	/// </summary>
	public bool RoundUp
	{
		get => _roundUp.Value;
		set => _roundUp.Value = value;
	}

	/// <summary>
	/// Candle type that supplies data for the ATR indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Last recommended volume after rounding and broker limits.
	/// </summary>
	public decimal RecommendedVolume => _recommendedVolume;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange
		{
			Length = 14
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ProcessAtr)
			.Start();
	}

	private void ProcessAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastAtrValue = atrValue;
		_hasAtr = atrValue > 0m;

		CalculateVolume();
	}

	private void CalculateVolume()
	{
		if (!_hasAtr)
			return;

		var security = Security;

		if (security is null)
			return;

		var portfolio = Portfolio;

		if (portfolio is null)
			return;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		var balance = portfolio.BeginValue ?? portfolio.CurrentValue ?? 0m;
		var basis = CalculationMode == AutoRiskCalculationMode.Balance ? balance : equity;

		if (basis <= 0m)
			return;

		var priceStep = security.PriceStep ?? security.Step ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return;

		var atrInSteps = _lastAtrValue / priceStep;

		if (atrInSteps <= 0m)
			return;

		var denominator = atrInSteps / stepPrice;

		if (denominator <= 0m)
			return;

		var rawVolume = RiskFactor * (basis / (denominator * 100m));

		var adjustedVolume = AdjustVolume(rawVolume);

		if (adjustedVolume <= 0m)
			return;

		_recommendedVolume = adjustedVolume;

		AddInfoLog($"Recommended volume: {_recommendedVolume:0.####}");
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;

		if (security is null)
			return 0m;

		var step = security.VolumeStep;
		var minVolume = security.MinVolume;
		var maxVolume = security.MaxVolume;

		if (step is null || step <= 0m)
			step = minVolume;

		if (step is { } stepValue && stepValue > 0m)
		{
			var multiplier = volume / stepValue;

			volume = RoundUp
				? Math.Round(multiplier, MidpointRounding.AwayFromZero) * stepValue
				: Math.Floor(multiplier) * stepValue;
		}

		if (minVolume is { } min && min > 0m && volume < min)
			volume = RoundUp ? min : 0m;

		if (maxVolume is { } max && max > 0m && volume > max)
			volume = max;

		return volume;
	}
}

/// <summary>
/// Available account metrics for the AutoRisk sizing logic.
/// </summary>
public enum AutoRiskCalculationMode
{
	/// <summary>
	/// Uses portfolio equity (current value) for the calculation.
	/// </summary>
	Equity,

	/// <summary>
	/// Uses portfolio balance (initial value) for the calculation.
	/// </summary>
	Balance
}

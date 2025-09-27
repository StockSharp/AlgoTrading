using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates the maximum trade volume that keeps the loss under a VaR limit.
/// Mirrors the arithmetic of the original MetaTrader script that produced the <c>amd.OperationVolume</c> figure.
/// Logs the intermediate values so traders can validate the risk model inputs.
/// </summary>
public class VarRiskVolumeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _varLimit;
	private readonly StrategyParam<int> _varPoints;
	private readonly StrategyParam<bool> _logDetails;

	/// <summary>
	/// Maximum acceptable loss expressed in account currency.
	/// </summary>
	public decimal VarLimit
	{
		get => _varLimit.Value;
		set => _varLimit.Value = value;
	}

	/// <summary>
	/// Price distance expressed in points that defines the VaR scenario.
	/// </summary>
	public int VarPoints
	{
		get => _varPoints.Value;
		set => _varPoints.Value = value;
	}

	/// <summary>
	/// Enables verbose logging with all intermediate calculation results.
	/// </summary>
	public bool LogDetails
	{
		get => _logDetails.Value;
		set => _logDetails.Value = value;
	}

	/// <summary>
	/// Final position volume that satisfies the configured risk constraints.
	/// </summary>
	public decimal OperationVolume { get; private set; }

	/// <summary>
	/// Margin amount that can be allocated while respecting the VaR limit.
	/// </summary>
	public decimal? CalculatedMarginLimit { get; private set; }

	/// <summary>
	/// Maximum raw volume allowed before snapping to the exchange step.
	/// </summary>
	public decimal? CalculatedVolumeLimit { get; private set; }

	/// <summary>
	/// Number of positions that fit inside the VaR budget.
	/// </summary>
	public decimal? CalculatedVarPositions { get; private set; }

	/// <summary>
	/// Points needed to absorb the minimal margin.
	/// </summary>
	public decimal? CalculatedPositionPoints { get; private set; }

	/// <summary>
	/// Monetary value of one point for the smallest tradable volume.
	/// </summary>
	public decimal? CalculatedMinimalPoint { get; private set; }

	/// <summary>
	/// Margin required to hold the minimal volume.
	/// </summary>
	public decimal? CalculatedMinimalMargin { get; private set; }

	/// <summary>
	/// Tick value multiplied by the minimal volume.
	/// </summary>
	public decimal? CalculatedMinimalTick { get; private set; }

	/// <summary>
	/// Exchange reported margin for one lot.
	/// </summary>
	public decimal? CalculatedNominalMargin { get; private set; }

	/// <summary>
	/// Exchange reported tick value for one lot.
	/// </summary>
	public decimal? CalculatedNominalTick { get; private set; }

	/// <summary>
	/// Minimal volume allowed by the exchange.
	/// </summary>
	public decimal? CalculatedMinimalVolume { get; private set; }

	/// <summary>
	/// Volume step used to align the calculated value.
	/// </summary>
	public decimal? CalculatedVolumeStep { get; private set; }

	/// <summary>
	/// Tick size reported for the security.
	/// </summary>
	public decimal? CalculatedQuoteTick { get; private set; }

	/// <summary>
	/// Point size used in the VaR conversion.
	/// </summary>
	public decimal? CalculatedQuotePoint { get; private set; }

	/// <summary>
	/// True when all required market parameters were present.
	/// </summary>
	public bool IsDataSufficient { get; private set; }

	/// <summary>
	/// List of security parameters that were missing during the last calculation.
	/// </summary>
	public IReadOnlyList<string> MissingFields { get; private set; } = Array.Empty<string>();

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public VarRiskVolumeStrategy()
	{
		_varLimit = Param(nameof(VarLimit), 200m)
			.SetGreaterThanZero()
			.SetDisplay("VaR Limit", "Maximum permitted loss expressed in account currency.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 1000m, 50m);

		_varPoints = Param(nameof(VarPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("VaR Points", "Price distance measured in points for the risk scenario.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 300, 10);

		_logDetails = Param(nameof(LogDetails), true)
			.SetDisplay("Log Details", "Write all intermediate values to the log for verification.", "Logging");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		OperationVolume = 0m;
		CalculatedMarginLimit = null;
		CalculatedVolumeLimit = null;
		CalculatedVarPositions = null;
		CalculatedPositionPoints = null;
		CalculatedMinimalPoint = null;
		CalculatedMinimalMargin = null;
		CalculatedMinimalTick = null;
		CalculatedNominalMargin = null;
		CalculatedNominalTick = null;
		CalculatedMinimalVolume = null;
		CalculatedVolumeStep = null;
		CalculatedQuoteTick = null;
		CalculatedQuotePoint = null;
		IsDataSufficient = false;
		MissingFields = Array.Empty<string>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		RecalculateOperationVolume();

		StartProtection();
	}

	private void RecalculateOperationVolume()
	{
		var security = Security;

		if (security == null)
		{
			LogWarn("Security is not assigned. Unable to calculate operation volume.");
			return;
		}

		var result = CalculateOperationVolume(security, VarLimit, VarPoints);

		OperationVolume = result.OperationVolume;
		CalculatedMarginLimit = result.MarginLimit;
		CalculatedVolumeLimit = result.VolumeLimit;
		CalculatedVarPositions = result.VarPositions;
		CalculatedPositionPoints = result.PositionPoints;
		CalculatedMinimalPoint = result.MinimalPoint;
		CalculatedMinimalMargin = result.MinimalMargin;
		CalculatedMinimalTick = result.MinimalTick;
		CalculatedNominalMargin = result.NominalMargin;
		CalculatedNominalTick = result.NominalTick;
		CalculatedMinimalVolume = result.MinimalVolume;
		CalculatedVolumeStep = result.VolumeStep;
		CalculatedQuoteTick = result.QuoteTick;
		CalculatedQuotePoint = result.QuotePoint;
		IsDataSufficient = result.IsDataSufficient;
		MissingFields = result.MissingFields;

		if (OperationVolume > 0m)
		{
			Volume = OperationVolume;
			LogInfo($"Operation volume = {OperationVolume:0.####} lots.");
		}
		else
		{
			LogWarn("Operation volume could not be determined with the available market parameters.");
		}

		if (LogDetails)
		{
			LogDetailedOutput(security, result);
		}

		if (!result.IsDataSufficient)
		{
			if (result.MissingFields.Count > 0)
			{
				LogWarn($"Missing security parameters: {string.Join(", ", result.MissingFields)}.");
			}
			else
			{
				LogWarn("Some security parameters are missing. Check margin, tick size and minimum volume settings.");
			}
		}
	}

	private void LogDetailedOutput(Security security, OperationVolumeResult result)
	{
		var portfolio = Portfolio;
		var accountCurrency = portfolio?.Currency ?? "N/A";
		var securityCurrency = security.Currency ?? "N/A";

		LogInfo(string.Empty);
		LogInfo($"Computed operation volume = {result.OperationVolume:0.####} lots.");
		LogInfo("Output:");
		LogInfo(string.Empty);
		LogInfo($"Minimal volume = {result.MinimalVolume:0.####} lots.");
		LogInfo($"Volume step = {result.VolumeStep:0.####} lots.");
		LogInfo($"Volume limit = {result.VolumeLimit:0.####} lots.");
		LogInfo($"Margin limit = {result.MarginLimit:0.####} {accountCurrency}.");
		LogInfo($"VaR limit = {VarLimit:0.####} {accountCurrency}.");
		LogInfo($"VaR positions = {result.VarPositions:0.####} positions.");
		LogInfo($"VaR points = {VarPoints} points.");
		LogInfo($"Position points = {result.PositionPoints:0.####} points.");
		LogInfo($"Minimal point value = {result.MinimalPoint:0.####} {accountCurrency}.");
		LogInfo($"Minimal margin = {result.MinimalMargin:0.####} {accountCurrency}.");
		LogInfo($"Nominal margin = {result.NominalMargin:0.####} {accountCurrency}.");
		LogInfo($"Nominal tick value = {result.NominalTick:0.####} {accountCurrency}.");
		LogInfo($"Quote tick size = {result.QuoteTick:0.####} price units.");
		LogInfo($"Quote point size = {result.QuotePoint:0.####} price units.");
		LogInfo("Processing:");
		LogInfo(string.Empty);
		LogInfo($"Input VaR points = {VarPoints} points.");
		LogInfo($"Input VaR limit = {VarLimit:0.####} {accountCurrency}.");
		LogInfo($"Symbol = {security.Id}.");
		LogInfo("Input:");
		LogInfo(string.Empty);
		LogInfo($"Account currency = {accountCurrency}.");
		LogInfo($"Security currency = {securityCurrency}.");

		if (portfolio?.CurrentValue is decimal currentValue)
		{
			LogInfo($"Portfolio current value = {currentValue:0.####} {accountCurrency}.");
		}

		if (portfolio?.BeginValue is decimal beginValue)
		{
			LogInfo($"Portfolio starting value = {beginValue:0.####} {accountCurrency}.");
		}

		LogInfo(string.Empty);
	}

	private OperationVolumeResult CalculateOperationVolume(Security security, decimal varLimit, int varPoints)
	{
		var volumeStep = security.VolumeStep ?? 0m;
		if (volumeStep < 0m)
		{
			volumeStep = 0m;
		}

		var minimalVolume = security.MinVolume ?? 0m;
		if (minimalVolume < 0m)
		{
			minimalVolume = 0m;
		}

		if (minimalVolume <= 0m && volumeStep > 0m)
		{
			minimalVolume = volumeStep;
		}

		var nominalMargin = security.MarginBuy ?? security.MarginSell ?? security.MarginLimit ?? 0m;
		if (nominalMargin < 0m)
		{
			nominalMargin = 0m;
		}

		var nominalTick = security.StepPrice ?? 0m;
		if (nominalTick < 0m)
		{
			nominalTick = 0m;
		}

		var quoteTick = security.PriceStep ?? security.MinPriceStep ?? security.Step ?? 0m;
		if (quoteTick < 0m)
		{
			quoteTick = 0m;
		}

		var quotePoint = security.MinPriceStep ?? security.PriceStep ?? security.Step ?? 0m;
		if (quotePoint < 0m)
		{
			quotePoint = 0m;
		}

		var minimalMargin = nominalMargin * minimalVolume;
		var minimalTick = nominalTick * minimalVolume;

		var minimalPoint = 0m;
		if (quoteTick > 0m && quotePoint > 0m)
		{
			minimalPoint = minimalTick * quotePoint / quoteTick;
		}

		decimal positionPoints = 0m;
		if (minimalPoint > 0m)
		{
			positionPoints = Math.Round(minimalMargin / minimalPoint, 0, MidpointRounding.AwayFromZero);
		}

		decimal varPositions = 0m;
		if (positionPoints > 0m)
		{
			varPositions = (decimal)varPoints / positionPoints;
		}

		decimal marginLimit = 0m;
		if (varPositions > 0m)
		{
			marginLimit = varLimit / varPositions;
		}

		decimal volumeLimit = 0m;
		if (nominalMargin > 0m)
		{
			volumeLimit = marginLimit / nominalMargin;
		}

		decimal operationVolume = 0m;

		if (volumeLimit > 0m)
		{
			if (minimalVolume > 0m)
			{
				if (volumeLimit >= minimalVolume)
				{
					if (volumeStep > 0m)
					{
						var steps = decimal.Floor((volumeLimit - minimalVolume) / volumeStep);
						if (steps < 0m)
						{
							steps = 0m;
						}

						operationVolume = minimalVolume + volumeStep * steps;
					}
					else
					{
						operationVolume = volumeLimit;
					}
				}
				else if (volumeStep <= 0m)
				{
					operationVolume = volumeLimit;
				}
			}
			else
			{
				if (volumeStep > 0m)
				{
					var steps = decimal.Floor(volumeLimit / volumeStep);
					if (steps > 0m)
					{
						operationVolume = steps * volumeStep;
					}
				}
				else
				{
					operationVolume = volumeLimit;
				}
			}
		}

		if (security.MaxVolume is decimal maxVolume && maxVolume > 0m && operationVolume > maxVolume)
		{
			operationVolume = maxVolume;
		}

		var missing = new List<string>();

		if (minimalVolume <= 0m)
		{
			missing.Add(nameof(security.MinVolume));
		}

		if (nominalMargin <= 0m)
		{
			missing.Add("MarginBuy/MarginSell");
		}

		if (nominalTick <= 0m)
		{
			missing.Add(nameof(security.StepPrice));
		}

		if (quoteTick <= 0m)
		{
			missing.Add(nameof(security.PriceStep));
		}

		if (quotePoint <= 0m)
		{
			missing.Add(nameof(security.MinPriceStep));
		}

		var isDataSufficient = missing.Count == 0;

		return new OperationVolumeResult
		{
			OperationVolume = operationVolume,
			MinimalVolume = minimalVolume,
			VolumeStep = volumeStep,
			VolumeLimit = volumeLimit,
			MarginLimit = marginLimit,
			VarPositions = varPositions,
			PositionPoints = positionPoints,
			MinimalPoint = minimalPoint,
			MinimalMargin = minimalMargin,
			MinimalTick = minimalTick,
			NominalMargin = nominalMargin,
			NominalTick = nominalTick,
			QuoteTick = quoteTick,
			QuotePoint = quotePoint,
			IsDataSufficient = isDataSufficient,
			MissingFields = missing
		};
	}

	private sealed class OperationVolumeResult
	{
		public decimal OperationVolume { get; init; }
		public decimal MinimalVolume { get; init; }
		public decimal VolumeStep { get; init; }
		public decimal VolumeLimit { get; init; }
		public decimal MarginLimit { get; init; }
		public decimal VarPositions { get; init; }
		public decimal PositionPoints { get; init; }
		public decimal MinimalPoint { get; init; }
		public decimal MinimalMargin { get; init; }
		public decimal MinimalTick { get; init; }
		public decimal NominalMargin { get; init; }
		public decimal NominalTick { get; init; }
		public decimal QuoteTick { get; init; }
		public decimal QuotePoint { get; init; }
		public bool IsDataSufficient { get; init; }
		public IReadOnlyList<string> MissingFields { get; init; } = Array.Empty<string>();
	}
}

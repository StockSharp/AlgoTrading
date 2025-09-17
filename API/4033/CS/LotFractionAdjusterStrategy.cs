namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;

/// <summary>
/// Reproduces the afd.AdjustLotSize helper from the MetaTrader script.
/// </summary>
public class LotFractionAdjusterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _inputLotSize;
	private readonly StrategyParam<int> _fractions;

	private decimal _lotStep;
	private decimal _stepsInput;
	private decimal _stepsRounded;
	private decimal _stepsOutput;
	private decimal _adjustedLotSize;

	public LotFractionAdjusterStrategy()
	{
		_inputLotSize = Param(nameof(InputLotSize), 0.58m)
			.SetGreaterThanZero()
			.SetDisplay("Input lot size", "Raw lot size requested before adjustment.", "Money Management");

		_fractions = Param(nameof(Fractions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fractions", "Number of minimal lot steps that must fit in one order.", "Money Management");
	}

	public decimal InputLotSize
	{
		get => _inputLotSize.Value;
		set => _inputLotSize.Value = value;
	}

	public int Fractions
	{
		get => _fractions.Value;
		set => _fractions.Value = value;
	}

	public decimal AdjustedLotSize => _adjustedLotSize;

	public decimal LotStep => _lotStep;

	public decimal StepsInput => _stepsInput;

	public decimal StepsRounded => _stepsRounded;

	public decimal StepsOutput => _stepsOutput;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (!TryCalculateAdjustment(out var adjustedVolume))
		{
			AddWarningLog("Volume step is not available; lot size adjustment skipped.");
			return;
		}

		_adjustedLotSize = adjustedVolume;

		if (adjustedVolume > 0m)
		{
			Volume = adjustedVolume; // Synchronise helper order methods with the adjusted lot size.
		}

		this.AddInfoLog($"AdjustedLotSize={_adjustedLotSize}, StepsOutput={_stepsOutput}, StepsRounded={_stepsRounded}, StepsInput={_stepsInput}, LotStep={_lotStep}, Fractions={Fractions}, InputLotSize={InputLotSize}");
	}

	private bool TryCalculateAdjustment(out decimal adjustedVolume)
	{
		adjustedVolume = 0m;

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep <= 0m)
		{
			return false; // Cannot mimic MODE_LOTSTEP without a valid volume step from the security metadata.
		}

		_lotStep = volumeStep;

		var fractionSize = Fractions > 0 ? Fractions : 1;
		var fractionDecimal = (decimal)fractionSize;

		_stepsInput = InputLotSize / volumeStep;
		_stepsRounded = Math.Round(_stepsInput, MidpointRounding.AwayFromZero); // Match MetaTrader MathRound behaviour.
		_stepsOutput = Math.Floor(_stepsRounded / fractionDecimal) * fractionDecimal; // Enforce multiples of the requested fraction.
		adjustedVolume = _stepsOutput * volumeStep;

		return true;
	}
}

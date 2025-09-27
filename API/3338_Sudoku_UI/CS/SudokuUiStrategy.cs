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

using System.IO;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader script SudokuUI.
/// Translates the puzzle driven interface into a grid-style mean reversion strategy that reacts to SMA deviations.
/// </summary>
public class SudokuUiStrategy : Strategy
{
	private readonly StrategyParam<string> _puzzleDefinition;
	private readonly StrategyParam<int> _shufflingRandomSeed;
	private readonly StrategyParam<int> _compositionRandomSeed;
	private readonly StrategyParam<int> _shufflingCycles;
	private readonly StrategyParam<int> _eliminateLabel;
	private readonly StrategyParam<bool> _enableAutoUpdate;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _thresholdRange;
	private readonly StrategyParam<decimal> _neutralBand;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private decimal[] _offsets = Array.Empty<decimal>();
	private DateTime? _lastPuzzleDate;
	private decimal _previousDifference;
	private bool _hasPreviousDifference;

	/// <summary>
	/// Initializes a new instance of the <see cref="SudokuUiStrategy"/> class.
	/// </summary>
	public SudokuUiStrategy()
	{
		_puzzleDefinition = Param(nameof(PuzzleDefinition), string.Empty)
		.SetDisplay("Puzzle Source", "File path or raw digits describing the Sudoku board", "Puzzle");

		_shufflingRandomSeed = Param(nameof(ShufflingRandomSeed), -1)
		.SetDisplay("Shuffle Seed", "Seed used for reproducible puzzle shuffling", "Puzzle");

		_compositionRandomSeed = Param(nameof(CompositionRandomSeed), -1)
		.SetDisplay("Composition Seed", "Additional seed influencing the level layout", "Puzzle");

		_shufflingCycles = Param(nameof(ShufflingCycles), 100)
		.SetRange(1, 10000)
		.SetDisplay("Shuffle Cycles", "Number of passes used while shuffling the puzzle digits", "Puzzle");

		_eliminateLabel = Param(nameof(EliminateLabel), 0)
		.SetRange(0, 9)
		.SetDisplay("Eliminate Digit", "Digit removed from the grid while building trading levels", "Puzzle");

		_enableAutoUpdate = Param(nameof(EnableAutoUpdate), false)
		.SetDisplay("Auto Update", "Regenerate the puzzle driven levels when the session day changes", "Trading");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("SMA Period", "Length of the smoothing moving average", "Trading");

		_thresholdRange = Param(nameof(ThresholdRange), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Threshold Range", "Maximum absolute percentage distance from the SMA", "Trading");

		_neutralBand = Param(nameof(NeutralBand), 0.001m)
		.SetRange(0m, 1m)
		.SetDisplay("Neutral Band", "Neutral zone around the SMA that triggers flat position", "Trading");


		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for SMA and grid evaluation", "General");
	}

	/// <summary>
	/// Raw Sudoku definition used to derive trading levels.
	/// Accepts a file path or a direct string of digits.
	/// </summary>
	public string PuzzleDefinition
	{
		get => _puzzleDefinition.Value;
		set => _puzzleDefinition.Value = value;
	}

	/// <summary>
	/// Seed used while shuffling puzzle digits.
	/// </summary>
	public int ShufflingRandomSeed
	{
		get => _shufflingRandomSeed.Value;
		set => _shufflingRandomSeed.Value = value;
	}

	/// <summary>
	/// Additional seed applied after the primary shuffle seed.
	/// </summary>
	public int CompositionRandomSeed
	{
		get => _compositionRandomSeed.Value;
		set => _compositionRandomSeed.Value = value;
	}

	/// <summary>
	/// Number of shuffling passes applied to the digit pool.
	/// </summary>
	public int ShufflingCycles
	{
		get => _shufflingCycles.Value;
		set => _shufflingCycles.Value = value;
	}

	/// <summary>
	/// Digit removed from the Sudoku layout while deriving thresholds.
	/// </summary>
	public int EliminateLabel
	{
		get => _eliminateLabel.Value;
		set => _eliminateLabel.Value = value;
	}

	/// <summary>
	/// Enables daily regeneration of puzzle levels.
	/// </summary>
	public bool EnableAutoUpdate
	{
		get => _enableAutoUpdate.Value;
		set => _enableAutoUpdate.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Maximum absolute distance from the SMA, expressed as a fraction.
	/// </summary>
	public decimal ThresholdRange
	{
		get => _thresholdRange.Value;
		set => _thresholdRange.Value = value;
	}

	/// <summary>
	/// Neutral zone around zero deviation that closes open positions.
	/// </summary>
	public decimal NeutralBand
	{
		get => _neutralBand.Value;
		set => _neutralBand.Value = value;
	}


	/// <summary>
	/// Candle type used for incoming data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_offsets = Array.Empty<decimal>();
		_lastPuzzleDate = null;
		_hasPreviousDifference = false;
		_previousDifference = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_sma = new SimpleMovingAverage { Length = SmaPeriod };

		PreparePuzzle(time.Date);

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(_sma, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (smaValue == 0m)
		return;

		if (EnableAutoUpdate)
		{
		var candleDate = candle.CloseTime.Date;

		if (_lastPuzzleDate != candleDate)
		PreparePuzzle(candleDate);
		}

		if (_offsets.Length == 0)
		return;

		var close = candle.ClosePrice;

		if (close == 0m)
		return;

		var difference = (close - smaValue) / smaValue;

		if (Math.Abs(difference) <= NeutralBand)
		{
		if (Position != 0)
		ClosePosition();

		_previousDifference = difference;
		_hasPreviousDifference = true;
		return;
		}

		var longThreshold = _offsets.FirstOrDefault(o => o < 0m);
		var shortThreshold = _offsets.LastOrDefault(o => o > 0m);

		var volume = Volume;

		if (volume <= 0m)
		return;

		var canOpenLong = longThreshold < 0m && ((difference <= longThreshold && (!_hasPreviousDifference || _previousDifference > longThreshold)));
		var canOpenShort = shortThreshold > 0m && ((difference >= shortThreshold && (!_hasPreviousDifference || _previousDifference < shortThreshold)));

		if (canOpenLong && Position <= 0)
		{
		if (Position < 0)
		ClosePosition();

		BuyMarket(volume);
		}
		else if (canOpenShort && Position >= 0)
		{
		if (Position > 0)
		ClosePosition();

		SellMarket(volume);
		}

		_previousDifference = difference;
		_hasPreviousDifference = true;
	}

	private void PreparePuzzle(DateTime date)
	{
		var digits = LoadPuzzleDigits();

		if (digits.Count != 81)
		digits = GeneratePuzzleDigits(date);

		_offsets = BuildOffsets(digits);
		_lastPuzzleDate = date;
		_hasPreviousDifference = false;
		_previousDifference = 0m;
	}

	private List<int> LoadPuzzleDigits()
	{
		var result = new List<int>(81);

		var source = PuzzleDefinition;

		if (source.IsEmptyOrWhiteSpace())
			return result;

		string content = source;

		foreach (var ch in content)
		{
			if (!char.IsDigit(ch))
				continue;

			if (ch == '0')
				continue;

			result.Add(ch - '0');

			if (result.Count == 81)
				break;
		}

		if (result.Count != 81)
			result.Clear();

		return result;
	}

	private List<int> GeneratePuzzleDigits(DateTime date)
	{
		var digits = new List<int>(81);

		var baseSeed = ShufflingRandomSeed;

		if (baseSeed == -1)
		baseSeed = date.Year * 10000 + date.Month * 100 + date.Day;

		if (CompositionRandomSeed != -1)
		baseSeed ^= CompositionRandomSeed * 397;

		baseSeed ^= ShufflingCycles * 7919;
		baseSeed ^= EliminateLabel * 104729;

		if (baseSeed < 0)
		baseSeed = Math.Abs(baseSeed);

		var random = new Random(baseSeed);

		for (var block = 0; block < 9; block++)
		{
		var numbers = Enumerable.Range(1, 9).ToList();
		Shuffle(numbers, random);
		digits.AddRange(numbers);
		}

		var passes = Math.Max(1, ShufflingCycles);

		for (var cycle = 1; cycle < passes; cycle++)
		Shuffle(digits, random);

		return digits;
	}

	private decimal[] BuildOffsets(IReadOnlyList<int> digits)
	{
		var offsets = new List<decimal>(9);

		var eliminateDigit = EliminateLabel;

		for (var column = 0; column < 9; column++)
		{
			decimal sum = 0m;
		var count = 0;

		for (var row = 0; row < 9; row++)
		{
		var digit = digits[row * 9 + column];

		if (eliminateDigit is >= 1 and <= 9 && digit == eliminateDigit)
		continue;

		sum += digit;
		count++;
		}

		if (count == 0)
		continue;

		var average = sum / count;
		var normalized = (average - 5m) / 4m;
		var offset = Clamp(normalized, -1m, 1m) * ThresholdRange;

		offsets.Add(offset);
		}

		offsets.Sort();

		if (offsets.Count == 0)
		return Array.Empty<decimal>();

		if (!offsets.Any(o => o < 0m))
		offsets.Insert(0, -ThresholdRange / 2m);

		if (!offsets.Any(o => o > 0m))
		offsets.Add(ThresholdRange / 2m);

		return offsets.ToArray();
	}

	private static void Shuffle<T>(IList<T> items, Random random)
	{
		for (var i = items.Count - 1; i > 0; i--)
		{
		var j = random.Next(i + 1);
		(items[i], items[j]) = (items[j], items[i]);
		}
	}

	private static decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (value < min)
		return min;

		if (value > max)
		return max;

		return value;
	}
}


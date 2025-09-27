using System;
using System.Collections.Generic;
using System.IO;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the Stat Euclidean Metric MetaTrader expert advisor.
/// It trades MACD reversals and optionally filters entries with a k-NN classifier
/// trained on moving average ratios loaded from binary data files.
/// </summary>
public class StatEuclideanMetricStrategy : Strategy
{
	private readonly StrategyParam<int> _featureCount;
	private readonly StrategyParam<int> _recordLength;

	private readonly StrategyParam<bool> _trainingMode;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<bool> _allowInverseEntries;
	private readonly StrategyParam<decimal> _inverseBuyThreshold;
	private readonly StrategyParam<decimal> _inverseSellThreshold;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<bool> _closePositionsOnSignal;
	private readonly StrategyParam<string> _buyDatasetPath;
	private readonly StrategyParam<string> _sellDatasetPath;
	private readonly StrategyParam<int> _neighborCount;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private SimpleMovingAverage _ma2 = null!;
	private SimpleMovingAverage _ma21 = null!;
	private SimpleMovingAverage _ma55 = null!;
	private SimpleMovingAverage _ma89 = null!;
	private SimpleMovingAverage _ma144 = null!;
	private SimpleMovingAverage _ma233 = null!;

	private readonly List<decimal> _macdHistory = new();
	private decimal[] _featureVector = Array.Empty<decimal>();

	private decimal[][] _buyDataset = Array.Empty<decimal[]>();
	private decimal[][] _sellDataset = Array.Empty<decimal[]>();
	private double[] _nearestDistances = Array.Empty<double>();
	private decimal[] _nearestLabels = Array.Empty<decimal>();

	/// <summary>
	/// Enables the training mode that mimics the base Expert Advisor behaviour.
	/// </summary>
	public bool TrainingMode
	{
		get => _trainingMode.Value;
		set => _trainingMode.Value = value;
	}

	/// <summary>
	/// Probability threshold for entering long trades when the classifier approves the setup.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Probability threshold for entering short trades when the classifier approves the setup.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Allows opening an opposite position when the probability is below the inverse threshold.
	/// </summary>
	public bool AllowInverseEntries
	{
		get => _allowInverseEntries.Value;
		set => _allowInverseEntries.Value = value;
	}

	/// <summary>
	/// Maximum probability for opening an opposite short position after a bullish setup.
	/// </summary>
	public decimal InverseBuyThreshold
	{
		get => _inverseBuyThreshold.Value;
		set => _inverseBuyThreshold.Value = value;
	}

	/// <summary>
	/// Maximum probability for opening an opposite long position after a bearish setup.
	/// </summary>
	public decimal InverseSellThreshold
	{
		get => _inverseSellThreshold.Value;
		set => _inverseSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the MACD indicator.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the MACD indicator.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length used by the MACD indicator.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in instrument steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in instrument steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Closes the current net position before sending a new signal-driven order.
	/// </summary>
	public bool ClosePositionsOnSignal
	{
		get => _closePositionsOnSignal.Value;
		set => _closePositionsOnSignal.Value = value;
	}

	/// <summary>
	/// Path to the binary file that stores bullish feature vectors.
	/// </summary>
	public string BuyDatasetPath
	{
		get => _buyDatasetPath.Value;
		set => _buyDatasetPath.Value = value;
	}

	/// <summary>
	/// Path to the binary file that stores bearish feature vectors.
	/// </summary>
	public string SellDatasetPath
	{
		get => _sellDatasetPath.Value;
		set => _sellDatasetPath.Value = value;
	}

	/// <summary>
	/// Number of nearest neighbours used to evaluate the probability.
	/// </summary>
	public int NeighborCount
	{
		get => _neighborCount.Value;
		set => _neighborCount.Value = value;
	}

	/// <summary>
	/// Number of features stored in each dataset vector.
	/// </summary>
	public int FeatureCount
	{
		get => _featureCount.Value;
		set => _featureCount.Value = value;
	}

	/// <summary>
	/// Total length of the stored dataset records including the label.
	/// </summary>
	public int RecordLength
	{
		get => _recordLength.Value;
		set => _recordLength.Value = value;
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
	/// Initializes default parameters.
	/// </summary>
	public StatEuclideanMetricStrategy()
	{
		_trainingMode = Param(nameof(TrainingMode), false)
			.SetDisplay("Training Mode", "Use pure MACD logic without classifier", "General");

		_buyThreshold = Param(nameof(BuyThreshold), 0.6m)
			.SetRange(0m, 1m)
			.SetDisplay("Buy Threshold", "Minimum probability required for longs", "Classifier")
			.SetCanOptimize(true)
			.SetOptimize(0.3m, 0.9m, 0.1m);

		_sellThreshold = Param(nameof(SellThreshold), 0.6m)
			.SetRange(0m, 1m)
			.SetDisplay("Sell Threshold", "Minimum probability required for shorts", "Classifier")
			.SetCanOptimize(true)
			.SetOptimize(0.3m, 0.9m, 0.1m);

		_allowInverseEntries = Param(nameof(AllowInverseEntries), true)
			.SetDisplay("Allow Inverse Entries", "Enable contrarian trades when probability is very low", "Classifier");

		_inverseBuyThreshold = Param(nameof(InverseBuyThreshold), 0.3m)
			.SetRange(0m, 1m)
			.SetDisplay("Inverse Buy Threshold", "Maximum probability to open a short instead of a long", "Classifier")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_inverseSellThreshold = Param(nameof(InverseSellThreshold), 0.3m)
			.SetRange(0m, 1m)
			.SetDisplay("Inverse Sell Threshold", "Maximum probability to open a long instead of a short", "Classifier")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period used by MACD", "Indicator");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period used by MACD", "Indicator");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal EMA period used by MACD", "Indicator");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 30)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_closePositionsOnSignal = Param(nameof(ClosePositionsOnSignal), false)
			.SetDisplay("Close Before Entry", "Exit current position before processing a new signal", "General");

		_buyDatasetPath = Param(nameof(BuyDatasetPath), "Buy_Position.dat")
			.SetDisplay("Buy Dataset", "Binary file with historical bullish vectors", "Classifier");

		_sellDatasetPath = Param(nameof(SellDatasetPath), "Sell_Position.dat")
			.SetDisplay("Sell Dataset", "Binary file with historical bearish vectors", "Classifier");

		_neighborCount = Param(nameof(NeighborCount), 10)
			.SetGreaterThanZero()
			.SetDisplay("Neighbors", "Number of nearest neighbours", "Classifier")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_featureCount = Param(nameof(FeatureCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Feature Count", "Number of classifier features stored per record", "Classifier");

		_recordLength = Param(nameof(RecordLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Record Length", "Dataset values per record including label", "Classifier");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_macdHistory.Clear();
		_featureVector = new decimal[Math.Max(1, FeatureCount)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength, CandlePrice = CandlePrice.Typical },
				LongMa = { Length = SlowLength, CandlePrice = CandlePrice.Typical },
			},
			SignalMa = { Length = SignalLength, CandlePrice = CandlePrice.Typical }
		};

		_ma2 = new SimpleMovingAverage { Length = 2, CandlePrice = CandlePrice.Typical };
		_ma21 = new SimpleMovingAverage { Length = 21, CandlePrice = CandlePrice.Typical };
		_ma55 = new SimpleMovingAverage { Length = 55, CandlePrice = CandlePrice.Typical };
		_ma89 = new SimpleMovingAverage { Length = 89, CandlePrice = CandlePrice.Typical };
		_ma144 = new SimpleMovingAverage { Length = 144, CandlePrice = CandlePrice.Typical };
		_ma233 = new SimpleMovingAverage { Length = 233, CandlePrice = CandlePrice.Typical };

		_buyDataset = LoadDataset(BuyDatasetPath, "Buy");
		_sellDataset = LoadDataset(SellDatasetPath, "Sell");

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Step),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Step));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _ma2, _ma21, _ma55, _ma89, _ma144, _ma233, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}

		var macdArea = CreateChartArea("MACD");
		if (macdArea != null)
		{
			DrawIndicator(macdArea, _macd);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue macdValue,
		IIndicatorValue ma2Value,
		IIndicatorValue ma21Value,
		IIndicatorValue ma55Value,
		IIndicatorValue ma89Value,
		IIndicatorValue ma144Value,
		IIndicatorValue ma233Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdLine)
			return;

		_macdHistory.Add(macdLine);
		if (_macdHistory.Count > 3)
			_macdHistory.RemoveAt(0);

		if (_macdHistory.Count < 3)
			return;

		var macd1 = _macdHistory[^1];
		var macd2 = _macdHistory[^2];
		var macd3 = _macdHistory[^3];

		var sellSetup = macd3 <= macd2 && macd2 > macd1;
		var buySetup = macd3 >= macd2 && macd2 < macd1;

		if (!sellSetup && !buySetup)
			return;

		if (TrainingMode)
		{
			ProcessTrainingMode(buySetup, sellSetup);
			return;
		}

		if (!ma2Value.IsFinal || !ma21Value.IsFinal || !ma55Value.IsFinal || !ma89Value.IsFinal || !ma144Value.IsFinal || !ma233Value.IsFinal)
			return;

		if (!_ma2.IsFormed || !_ma21.IsFormed || !_ma55.IsFormed || !_ma89.IsFormed || !_ma144.IsFormed || !_ma233.IsFormed)
			return;

		var ma2 = ma2Value.ToDecimal();
		var ma21 = ma21Value.ToDecimal();
		var ma55 = ma55Value.ToDecimal();
		var ma89 = ma89Value.ToDecimal();
		var ma144 = ma144Value.ToDecimal();
		var ma233 = ma233Value.ToDecimal();

		if (ma144 == 0m || ma233 == 0m || ma89 == 0m || ma55 == 0m)
			return;

		if (_featureVector.Length != FeatureCount)
			_featureVector = new decimal[Math.Max(1, FeatureCount)];

		var featureValues = new[]
		{
			ma89 / ma144,
			ma144 / ma233,
			ma21 / ma89,
			ma55 / ma89,
			ma2 / ma55
		};

		for (var i = 0; i < _featureVector.Length; i++)
			_featureVector[i] = i < featureValues.Length ? featureValues[i] : 0m;

		if (sellSetup)
			ProcessClassifierSignal(_sellDataset, SellThreshold, InverseSellThreshold, true);

		if (buySetup)
			ProcessClassifierSignal(_buyDataset, BuyThreshold, InverseBuyThreshold, false);
	}

	private void ProcessTrainingMode(bool buySetup, bool sellSetup)
	{
		if (sellSetup)
		{
			if (ClosePositionsOnSignal)
				ClosePosition();

			OpenShort();
		}

		if (buySetup)
		{
			if (ClosePositionsOnSignal)
				ClosePosition();

			OpenLong();
		}
	}

	private void ProcessClassifierSignal(decimal[][] dataset, decimal entryThreshold, decimal inverseThreshold, bool isSellSignal)
	{
		var probability = EvaluateProbability(dataset, _featureVector);

		if (probability >= entryThreshold)
		{
			if (ClosePositionsOnSignal)
				ClosePosition();

			if (isSellSignal)
				OpenShort();
			else
				OpenLong();
		}
		else if (AllowInverseEntries && probability <= inverseThreshold)
		{
			if (ClosePositionsOnSignal)
				ClosePosition();

			if (isSellSignal)
				OpenLong();
			else
				OpenShort();
		}
	}

	private void OpenLong()
	{
		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
			BuyMarket(volume);
	}

	private void OpenShort()
	{
		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
			SellMarket(volume);
	}

	private void ClosePosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private decimal EvaluateProbability(decimal[][] dataset, IReadOnlyList<decimal> vector)
	{
		var neighbourLimit = Math.Min(NeighborCount, dataset.Length);
		if (neighbourLimit <= 0)
			return 0m;

		EnsureNeighborBuffers(neighbourLimit);
		for (var i = 0; i < neighbourLimit; i++)
		{
			_nearestDistances[i] = double.PositiveInfinity;
			_nearestLabels[i] = 0m;
		}

		var actualNeighbours = 0;

		foreach (var record in dataset)
		{
			if (RecordLength < 2 || record.Length < RecordLength)
				continue;

			var labelIndex = Math.Min(record.Length - 1, RecordLength - 1);
			var featureLimit = Math.Min(FeatureCount, labelIndex);
			if (featureLimit <= 0)
				continue;

			var distance = 0d;
			for (var i = 0; i < featureLimit; i++)
			{
				var diff = (double)(record[i] - vector[i]);
				distance += diff * diff;
			}

			distance = Math.Sqrt(distance);
			var inserted = false;

			for (var i = 0; i < neighbourLimit; i++)
			{
				if (distance < _nearestDistances[i])
				{
					for (var j = Math.Min(actualNeighbours, neighbourLimit - 1); j > i; j--)
					{
						_nearestDistances[j] = _nearestDistances[j - 1];
						_nearestLabels[j] = _nearestLabels[j - 1];
					}

					_nearestDistances[i] = distance;
					_nearestLabels[i] = record[labelIndex];
					if (actualNeighbours < neighbourLimit)
						actualNeighbours++;
					inserted = true;
					break;
				}
			}

			if (!inserted && actualNeighbours < neighbourLimit)
			{
				_nearestDistances[actualNeighbours] = distance;
				_nearestLabels[actualNeighbours] = record[labelIndex];
				actualNeighbours++;
			}
		}

		if (actualNeighbours == 0)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < actualNeighbours; i++)
			sum += _nearestLabels[i];

		return sum / actualNeighbours;
	}

	private void EnsureNeighborBuffers(int count)
	{
		if (_nearestDistances.Length != count)
		{
			Array.Resize(ref _nearestDistances, count);
		}

		if (_nearestLabels.Length != count)
		{
			Array.Resize(ref _nearestLabels, count);
		}
	}

	private decimal[][] LoadDataset(string path, string name)
	{
		if (path.IsEmptyOrWhiteSpace())
			return Array.Empty<decimal[]>();

		var fullPath = Path.IsPathRooted(path)
			? path
			: Path.Combine(Environment.CurrentDirectory, path);

		if (!File.Exists(fullPath))
		{
			LogInfo($"{name} dataset file '{fullPath}' not found. Classifier will use empty data set.");
			return Array.Empty<decimal[]>();
		}

		try
		{
			var records = new List<decimal[]>();
			using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var reader = new BinaryReader(stream);

			var recordLength = RecordLength;
			if (recordLength <= 0)
			{
				LogInfo($"{name} dataset ignored because record length parameter is not positive.");
				return Array.Empty<decimal[]>();
			}

			while (stream.Position + sizeof(double) * recordLength <= stream.Length)
			{
				var values = new decimal[recordLength];
				for (var i = 0; i < recordLength; i++)
				{
					values[i] = (decimal)reader.ReadDouble();
				}

				records.Add(values);
			}

			LogInfo($"{name} dataset loaded with {records.Count} vectors from '{fullPath}'.");
			return records.ToArray();
		}
		catch (Exception ex)
		{
			LogError($"Failed to load {name} dataset from '{fullPath}': {ex.Message}");
			return Array.Empty<decimal[]>();
		}
	}
}

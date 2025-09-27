using System;
using System.Collections.Generic;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Self-organizing map strategy that evaluates Tom DeMark pivot vectors.
/// The map learns from recent opens and classifies the next move into buy, sell or hold.
/// </summary>
public class RichKohonenMapStrategy : Strategy
{
private readonly StrategyParam<int> _vectorSize;
private readonly StrategyParam<int> _mapBase;
private readonly StrategyParam<int> _holdBase;
private readonly StrategyParam<int> _historyLength;

private readonly StrategyParam<int> _minPips;
private readonly StrategyParam<int> _maxPips;
private readonly StrategyParam<int> _takeProfit;
private readonly StrategyParam<int> _stopLoss;
private readonly StrategyParam<decimal> _lots;
private readonly StrategyParam<string> _mapPath;
private readonly StrategyParam<string> _eaName;
private readonly StrategyParam<DataType> _candleType;

private readonly List<ICandleMessage> _candles = new();
private double[,] _mapBuy = null!;
private double[,] _mapSell = null!;
private double[,] _mapHold = null!;
private double[] _currentVector = null!;
private double[] _previousVector = null!;

	private int _buyCount;
	private int _sellCount;
	private int _holdCount;

	private enum MapAction
	{
		Buy,
		Sell,
		Hold,
	}

/// <summary>
/// Number of values stored in each training vector.
/// </summary>
public int VectorSize
{
get => _vectorSize.Value;
set => _vectorSize.Value = value;
}

/// <summary>
/// Maximum number of samples stored in the buy/sell maps.
/// </summary>
public int MapBase
{
get => _mapBase.Value;
set => _mapBase.Value = value;
}

/// <summary>
/// Maximum number of samples stored in the hold map.
/// </summary>
public int HoldBase
{
get => _holdBase.Value;
set => _holdBase.Value = value;
}

/// <summary>
/// Number of candles forming the feature vector.
/// </summary>
public int HistoryLength
{
get => _historyLength.Value;
set => _historyLength.Value = value;
}

/// <summary>
/// Minimum pip distance to register a buy training example.
/// </summary>
public int MinPips
{
get => _minPips.Value;
set => _minPips.Value = value;
}

	/// <summary>
	/// Maximum pip distance to register a buy or sell training example.
	/// </summary>
	public int MaxPips
	{
		get => _maxPips.Value;
		set => _maxPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips (kept for parity with the original expert advisor).
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in pips (kept for parity with the original expert advisor).
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Base lot size used when the balance driven size is not available.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Path to the binary file containing the persistent map memory.
	/// </summary>
	public string MapPath
	{
		get => _mapPath.Value;
		set => _mapPath.Value = value;
	}

	/// <summary>
	/// Comment recorded on orders (kept for reference).
	/// </summary>
	public string EAName
	{
		get => _eaName.Value;
		set => _eaName.Value = value;
	}

	/// <summary>
	/// Candle type used for vector calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

/// <summary>
/// Initializes strategy parameters.
/// </summary>
	public RichKohonenMapStrategy()
	{
		_vectorSize = Param(nameof(VectorSize), 7)
.SetGreaterOrEqual(7)
.SetDisplay("Vector Size", "Number of values stored in each training vector", "Training")
.SetCanOptimize(true);

		_mapBase = Param(nameof(MapBase), 10000)
	                .SetGreaterThanZero()
	                .SetDisplay("Map Capacity", "Maximum number of samples stored in the buy/sell maps", "Training");

		_holdBase = Param(nameof(HoldBase), 25000)
	                .SetGreaterThanZero()
	                .SetDisplay("Hold Capacity", "Maximum number of samples stored in the hold map", "Training");

		_historyLength = Param(nameof(HistoryLength), 7)
.SetGreaterOrEqual(7)
.SetDisplay("History Length", "Number of candles forming the feature vector", "Training")
.SetCanOptimize(true);

		_minPips = Param(nameof(MinPips), 5)
	                .SetGreaterThanZero()
	                .SetDisplay("Min Pips", "Minimum pip distance for buy samples", "Training")
	                .SetCanOptimize(true)
	                .SetOptimize(1, 50, 1);

		_maxPips = Param(nameof(MaxPips), 43)
			.SetGreaterThanZero()
			.SetDisplay("Max Pips", "Maximum pip distance for buy/sell samples", "Training")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 5);

		_takeProfit = Param(nameof(TakeProfit), 150)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Legacy take profit in pips", "Execution");

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Legacy stop loss in pips", "Execution");

		_lots = Param(nameof(Lots), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lots", "Fallback order size", "Execution")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_mapPath = Param(nameof(MapPath), "rl.bin")
			.SetDisplay("Map Path", "Binary storage for the Kohonen map", "Storage");

		_eaName = Param(nameof(EAName), "RowLearner")
			.SetDisplay("EA Name", "Comment stored on generated orders", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Data");
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
		_candles.Clear();
		ResetMaps();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetMaps();
		LoadKohonenMap();

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		SaveKohonenMap();
		base.OnStopped();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Maintain a rolling window of the last 7 candles.
		_candles.Add(candle);
		if (_candles.Count > HistoryLength)
			_candles.RemoveAt(0);

		if (_candles.Count < HistoryLength)
			return;

		// Update the feature vectors for the current and previous bars.
		UpdateFeatureVectors();

		var buyDistance = FindBestMatchingUnit(_mapBuy, _buyCount, _currentVector);
		var sellDistance = FindBestMatchingUnit(_mapSell, _sellCount, _currentVector);
		var holdDistance = FindBestMatchingUnit(_mapHold, _holdCount, _currentVector);

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var volume = CalculateVolume();
			ExecuteSignal(buyDistance, sellDistance, holdDistance, volume);
		}

		var pipDifference = GetOpenDifferenceInPips();
		TeachMapBasedOnMove(pipDifference);
	}

	private void ExecuteSignal(double buyDistance, double sellDistance, double holdDistance, decimal volume)
	{
		// Determine which map has the smallest distance.
		var action = MapAction.Buy;
		var best = buyDistance;

		if (sellDistance < best)
		{
			best = sellDistance;
			action = MapAction.Sell;
		}

		if (holdDistance < best)
			action = MapAction.Hold;

		var targetPosition = action switch
		{
			MapAction.Buy => volume,
			MapAction.Sell => -volume,
			_ => 0m,
		};

		var diff = targetPosition - Position;

		if (diff > 0m)
		{
			// Positive diff means we need to buy additional volume.
			BuyMarket(diff);
		}
		else if (diff < 0m)
		{
			// Negative diff means we need to sell to reach the target position.
			SellMarket(-diff);
		}
	}

	private void TeachMapBasedOnMove(decimal pipDifference)
	{
		var min = (decimal)MinPips;
		var max = (decimal)MaxPips;

		if (pipDifference >= min && pipDifference <= max)
		{
			TeachMap(MapAction.Buy, _previousVector);
		}
		else if (pipDifference <= -min && pipDifference >= -max)
		{
			TeachMap(MapAction.Sell, _previousVector);
		}
		else
		{
			TeachMap(MapAction.Hold, _previousVector);
		}
	}

	private void TeachMap(MapAction action, double[] vector)
	{
		switch (action)
		{
			case MapAction.Buy:
				AddVector(_mapBuy, ref _buyCount, MapBase, vector);
				break;
			case MapAction.Sell:
				AddVector(_mapSell, ref _sellCount, MapBase, vector);
				break;
			default:
				AddVector(_mapHold, ref _holdCount, HoldBase, vector);
				break;
		}
	}

	private void AddVector(double[,] map, ref int count, int capacity, double[] vector)
	{
	        if (count >= capacity)
	                return;

	        for (var i = 0; i < VectorSize; i++)
	                map[count, i] = vector[i];

	        count++;
	}

	private double FindBestMatchingUnit(double[,] map, int count, double[] vector)
	{
	        var best = double.MaxValue;

	        for (var i = 0; i < count; i++)
	        {
	                var distance = 0d;

	                for (var v = 0; v < VectorSize; v++)
	                {
	                        var diff = map[i, v] * 10000d - vector[v] * 10000d;
	                        distance += diff * diff;
	                }

	                distance = Math.Sqrt(distance);

	                if (distance < best)
	                        best = distance;
	        }

	        return best;
	}

	private void UpdateFeatureVectors()
	{
		FillVector(_currentVector, 1);
		FillVector(_previousVector, 2);
	}

	private void FillVector(double[] target, int startOffset)
	{
		var open = (double)GetCandleFromEnd(startOffset - 1).OpenPrice;
		var close = (double)GetCandleFromEnd(startOffset).ClosePrice;

		var highest = double.MinValue;
		var lowest = double.MaxValue;

		for (var i = 0; i < 5; i++)
		{
			var high = (double)GetCandleFromEnd(startOffset + i).HighPrice;
			var low = (double)GetCandleFromEnd(startOffset + i).LowPrice;

			if (high > highest)
				highest = high;

			if (low < lowest)
				lowest = low;
		}

		var pivot = (highest + lowest + close) / 3d;

		target[0] = pivot - open;
		target[1] = (2d * pivot - lowest) - open;
		target[2] = (pivot + highest - lowest) - open;
		target[3] = (highest + 2d * (pivot - lowest)) - open;
		target[4] = (2d * pivot - highest) - open;
		target[5] = (pivot - highest + lowest) - open;
		target[6] = (lowest - 2d * (highest - pivot)) - open;
	}

	private ICandleMessage GetCandleFromEnd(int offset)
	{
		var index = _candles.Count - 1 - offset;
		return _candles[index];
	}

	private decimal CalculateVolume()
	{
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var dynamic = balance > 0m ? Math.Floor(balance / 50m) / 10m : 0m;

		if (dynamic <= 0m)
			dynamic = Lots;

		return Math.Max(dynamic, 0m);
	}

	private decimal GetOpenDifferenceInPips()
	{
		var currentOpen = _candles[^1].OpenPrice;
		var previousOpen = _candles[^2].OpenPrice;
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			step = 1m;

		return (currentOpen - previousOpen) / step;
	}

	private void ResetMaps()
	{
	        AllocateBuffers();
	        Array.Clear(_mapBuy, 0, _mapBuy.Length);
	        Array.Clear(_mapSell, 0, _mapSell.Length);
	        Array.Clear(_mapHold, 0, _mapHold.Length);
	        Array.Clear(_currentVector, 0, _currentVector.Length);
	        Array.Clear(_previousVector, 0, _previousVector.Length);
		_buyCount = 0;
		_sellCount = 0;
		_holdCount = 0;
	}

	private void LoadKohonenMap()
	{
	        var path = GetMapFullPath();

	        if (path.IsEmptyOrWhiteSpace() || !File.Exists(path))
	                return;

	        try
	        {
	                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
	                using var reader = new BinaryReader(stream);

	                ReadMap(reader, _mapBuy, MapBase);
	                ReadMap(reader, _mapSell, MapBase);
	                ReadMap(reader, _mapHold, HoldBase);
	        }
	        catch (Exception ex)
	        {
	                LogError($"Failed to load Kohonen map: {ex.Message}");
	        }
	        finally
	        {
	                _buyCount = CountFilledRows(_mapBuy, MapBase);
	                _sellCount = CountFilledRows(_mapSell, MapBase);
	                _holdCount = CountFilledRows(_mapHold, HoldBase);
	        }
	}

	private void SaveKohonenMap()
	{
	        var path = GetMapFullPath();

	        if (path.IsEmptyOrWhiteSpace())
	                return;

	        try
	        {
	                var directory = Path.GetDirectoryName(path);
	                if (!directory.IsEmpty())
	                        Directory.CreateDirectory(directory);

	                using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
	                using var writer = new BinaryWriter(stream);

	                WriteMap(writer, _mapBuy, MapBase);
	                WriteMap(writer, _mapSell, MapBase);
	                WriteMap(writer, _mapHold, HoldBase);
	        }
	        catch (Exception ex)
	        {
	                LogError($"Failed to save Kohonen map: {ex.Message}");
	        }
	}

	private string GetMapFullPath()
	{
	        if (MapPath.IsEmptyOrWhiteSpace())
	                return Path.Combine(Environment.CurrentDirectory, "rl.bin");

	        return Path.IsPathRooted(MapPath)
	                ? MapPath
	                : Path.Combine(Environment.CurrentDirectory, MapPath);
	}

	private void ReadMap(BinaryReader reader, double[,] map, int rows)
	{
	        for (var i = 0; i < rows; i++)
	        {
	                for (var v = 0; v < VectorSize; v++)
	                {
	                        if (reader.BaseStream.Position >= reader.BaseStream.Length)
	                                return;

	                        map[i, v] = reader.ReadDouble();
	                }
	        }
	}

	private void WriteMap(BinaryWriter writer, double[,] map, int rows)
	{
	        for (var i = 0; i < rows; i++)
	        {
	                for (var v = 0; v < VectorSize; v++)
	                        writer.Write(map[i, v]);
	        }
	}

	private int CountFilledRows(double[,] map, int rows)
	{
	        var count = 0;

	        for (var i = 0; i < rows; i++)
	        {
	                var empty = true;

	                for (var v = 0; v < VectorSize; v++)
	                {
	                        if (map[i, v] != 0d)
	                        {
	                                empty = false;
	                                break;
	                        }
	                }

	                if (empty)
	                        break;

	                count++;
	        }

	        return count;
	}

	private void AllocateBuffers()
	{
	        var vectorSize = VectorSize;
	        var mapCapacity = MapBase;
	        var holdCapacity = HoldBase;

	        if (_mapBuy == null || _mapBuy.GetLength(0) != mapCapacity || _mapBuy.GetLength(1) != vectorSize)
	                _mapBuy = new double[mapCapacity, vectorSize];

	        if (_mapSell == null || _mapSell.GetLength(0) != mapCapacity || _mapSell.GetLength(1) != vectorSize)
	                _mapSell = new double[mapCapacity, vectorSize];

	        if (_mapHold == null || _mapHold.GetLength(0) != holdCapacity || _mapHold.GetLength(1) != vectorSize)
	                _mapHold = new double[holdCapacity, vectorSize];

	        if (_currentVector == null || _currentVector.Length != vectorSize)
	                _currentVector = new double[vectorSize];

	        if (_previousVector == null || _previousVector.Length != vectorSize)
	                _previousVector = new double[vectorSize];
	}
}

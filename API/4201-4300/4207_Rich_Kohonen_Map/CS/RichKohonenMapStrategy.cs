using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Self-organizing-map style classifier trained on DeMark-derived candle feature vectors.
/// </summary>
public class RichKohonenMapStrategy : Strategy
{
	private const int _vectorSize = 7;
	private const int _magic = 0x534B4D31;

	private readonly StrategyParam<decimal> _minPips;
	private readonly StrategyParam<decimal> _maxPips;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<string> _mapPath;
	private readonly StrategyParam<string> _eaName;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = [];
	private readonly List<double[]> _buyMap = [];
	private readonly List<double[]> _sellMap = [];
	private readonly List<double[]> _holdMap = [];

	public decimal MinPips { get => _minPips.Value; set => _minPips.Value = value; }
	public decimal MaxPips { get => _maxPips.Value; set => _maxPips.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal Lots { get => _lots.Value; set => _lots.Value = value; }
	public int Slippage { get => _slippage.Value; set => _slippage.Value = value; }
	public string MapPath { get => _mapPath.Value; set => _mapPath.Value = value; }
	public string EAName { get => _eaName.Value; set => _eaName.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RichKohonenMapStrategy()
	{
		_minPips = Param(nameof(MinPips), 10m).SetNotNegative().SetDisplay("Min Pips", "Minimum move classified as directional.", "Learning");
		_maxPips = Param(nameof(MaxPips), 100m).SetGreaterThanZero().SetDisplay("Max Pips", "Maximum move accepted as directional training sample.", "Learning");
		_takeProfit = Param(nameof(TakeProfit), 0m).SetNotNegative().SetDisplay("Take Profit", "Compatibility parameter from the original EA.", "Trading");
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative().SetDisplay("Stop Loss", "Compatibility parameter from the original EA.", "Trading");
		_lots = Param(nameof(Lots), 0.1m).SetGreaterThanZero().SetDisplay("Lots", "Fallback volume when balance-based sizing returns zero.", "Trading");
		_slippage = Param(nameof(Slippage), 3).SetNotNegative().SetDisplay("Slippage", "Compatibility slippage parameter.", "Trading");
		_mapPath = Param(nameof(MapPath), "rl.bin").SetDisplay("Map Path", "Binary persistence path for Kohonen maps.", "Learning");
		_eaName = Param(nameof(EAName), "Rich").SetDisplay("EA Name", "Informational EA name.", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Feature extraction timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_buyMap.Clear();
		_sellMap.Clear();
		_holdMap.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		LoadMaps();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		if (_candles.Count > 8)
			_candles.RemoveAt(0);

		if (_candles.Count < 7)
			return;

		var current = BuildVector(0);
		var previous = BuildVector(1);
		var decision = Classify(current);
		SetTarget(decision);

		var latest = _candles[^1];
		var prior = _candles[^2];
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var movePips = (latest.OpenPrice - prior.OpenPrice) / step;
		if (movePips >= MinPips && movePips <= MaxPips)
			AddPrototype(_buyMap, previous, 10000);
		else if (movePips <= -MinPips && movePips >= -MaxPips)
			AddPrototype(_sellMap, previous, 10000);
		else
			AddPrototype(_holdMap, previous, 25000);

		SaveMaps();
	}

	private int Classify(double[] vector)
	{
		var buyDistance = BestDistance(_buyMap, vector);
		var sellDistance = BestDistance(_sellMap, vector);
		var holdDistance = BestDistance(_holdMap, vector);

		if (double.IsPositiveInfinity(buyDistance) &&
			double.IsPositiveInfinity(sellDistance) &&
			double.IsPositiveInfinity(holdDistance))
			return 0;

		if (buyDistance <= sellDistance && buyDistance <= holdDistance)
			return 1;
		if (sellDistance <= buyDistance && sellDistance <= holdDistance)
			return -1;
		return 0;
	}

	private void SetTarget(int decision)
	{
		var volume = CalculateVolume();
		var target = decision * volume;
		var difference = target - Position;

		if (difference > 0m)
			BuyMarket(difference);
		else if (difference < 0m)
			SellMarket(Math.Abs(difference));
	}

	private decimal CalculateVolume()
	{
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var volume = balance > 0m ? Math.Floor(balance / 50m) / 10m : 0m;
		if (volume <= 0m)
			volume = Lots;

		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		return volume > 0m ? volume : Lots;
	}

	private double[] BuildVector(int offset)
	{
		var lastIndex = _candles.Count - 1 - offset;
		var current = _candles[lastIndex];

		double pivotSum = 0, r1Sum = 0, s1Sum = 0, rangeSum = 0, bodySum = 0, closeSum = 0;

		for (var i = lastIndex - 5; i < lastIndex; i++)
		{
			var c = _candles[i];
			var x = c.ClosePrice < c.OpenPrice
				? c.HighPrice + 2m * c.LowPrice + c.ClosePrice
				: c.ClosePrice > c.OpenPrice
					? 2m * c.HighPrice + c.LowPrice + c.ClosePrice
					: c.HighPrice + c.LowPrice + 2m * c.ClosePrice;

			var pivot = x / 4m;
			pivotSum += (double)pivot;
			r1Sum += (double)(x / 2m - c.LowPrice);
			s1Sum += (double)(x / 2m - c.HighPrice);
			rangeSum += (double)(c.HighPrice - c.LowPrice);
			bodySum += (double)(c.ClosePrice - c.OpenPrice);
			closeSum += (double)c.ClosePrice;
		}

		return
		[
			(double)current.OpenPrice,
			pivotSum / 5d,
			r1Sum / 5d,
			s1Sum / 5d,
			rangeSum / 5d,
			bodySum / 5d,
			closeSum / 5d,
		];
	}

	private static double BestDistance(List<double[]> map, double[] vector)
	{
		var best = double.PositiveInfinity;
		foreach (var prototype in map)
		{
			var sum = 0d;
			for (var i = 0; i < _vectorSize; i++)
			{
				var d = vector[i] - prototype[i];
				sum += d * d;
			}
			best = Math.Min(best, Math.Sqrt(sum));
		}
		return best;
	}

	private static void AddPrototype(List<double[]> map, double[] vector, int capacity)
	{
		// A light SOM update: the best matching unit is nudged toward the new sample;
		// until the map is full, the sample is also retained as a new prototype.
		if (map.Count > 0)
		{
			var bestIndex = 0;
			var bestDistance = double.PositiveInfinity;
			for (var i = 0; i < map.Count; i++)
			{
				var d = BestDistance([map[i]], vector);
				if (d < bestDistance)
				{
					bestDistance = d;
					bestIndex = i;
				}
			}

			for (var j = 0; j < _vectorSize; j++)
				map[bestIndex][j] += 0.05d * (vector[j] - map[bestIndex][j]);
		}

		if (map.Count < capacity)
			map.Add((double[])vector.Clone());
	}

	private void LoadMaps()
	{
		if (MapPath.IsEmpty() || !File.Exists(MapPath))
			return;

		try
		{
			using var reader = new BinaryReader(File.OpenRead(MapPath));
			if (reader.ReadInt32() != _magic)
				return;

			ReadMap(reader, _buyMap, 10000);
			ReadMap(reader, _sellMap, 10000);
			ReadMap(reader, _holdMap, 25000);
		}
		catch (Exception ex)
		{
			LogWarning($"Unable to load Kohonen map '{MapPath}': {ex.Message}");
		}
	}

	private void SaveMaps()
	{
		if (MapPath.IsEmpty())
			return;

		try
		{
			using var writer = new BinaryWriter(File.Create(MapPath));
			writer.Write(_magic);
			WriteMap(writer, _buyMap);
			WriteMap(writer, _sellMap);
			WriteMap(writer, _holdMap);
		}
		catch (Exception ex)
		{
			LogWarning($"Unable to save Kohonen map '{MapPath}': {ex.Message}");
		}
	}

	private static void ReadMap(BinaryReader reader, List<double[]> map, int capacity)
	{
		var count = Math.Min(reader.ReadInt32(), capacity);
		for (var i = 0; i < count; i++)
		{
			var vector = new double[_vectorSize];
			for (var j = 0; j < _vectorSize; j++)
				vector[j] = reader.ReadDouble();
			map.Add(vector);
		}
	}

	private static void WriteMap(BinaryWriter writer, List<double[]> map)
	{
		writer.Write(map.Count);
		foreach (var vector in map)
			for (var i = 0; i < _vectorSize; i++)
				writer.Write(vector[i]);
	}
}

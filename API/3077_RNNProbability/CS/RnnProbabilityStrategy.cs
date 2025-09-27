namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Probabilistic strategy converted from the RNN MetaTrader expert.
/// It feeds three delayed RSI readings into the original probability lattice and
/// trades in the direction suggested by the neural network output.
/// </summary>
public class RnnProbabilityStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<decimal> _stopLossTakeProfitPips;
	private readonly StrategyParam<decimal> _weight0;
	private readonly StrategyParam<decimal> _weight1;
	private readonly StrategyParam<decimal> _weight2;
	private readonly StrategyParam<decimal> _weight3;
	private readonly StrategyParam<decimal> _weight4;
	private readonly StrategyParam<decimal> _weight5;
	private readonly StrategyParam<decimal> _weight6;
	private readonly StrategyParam<decimal> _weight7;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private readonly List<decimal> _rsiHistory = new();
	private decimal _pipSize;

	/// <summary>
	/// Trade volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Averaging period for the RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source forwarded to the RSI indicator.
	/// </summary>
	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Symmetric stop-loss and take-profit distance expressed in pips.
	/// </summary>
	public decimal StopLossTakeProfitPips
	{
		get => _stopLossTakeProfitPips.Value;
		set => _stopLossTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (low, low, low) RSI combination.
	/// </summary>
	public decimal Weight0
	{
		get => _weight0.Value;
		set => _weight0.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (low, low, high) RSI combination.
	/// </summary>
	public decimal Weight1
	{
		get => _weight1.Value;
		set => _weight1.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (low, high, low) RSI combination.
	/// </summary>
	public decimal Weight2
	{
		get => _weight2.Value;
		set => _weight2.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (low, high, high) RSI combination.
	/// </summary>
	public decimal Weight3
	{
		get => _weight3.Value;
		set => _weight3.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (high, low, low) RSI combination.
	/// </summary>
	public decimal Weight4
	{
		get => _weight4.Value;
		set => _weight4.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (high, low, high) RSI combination.
	/// </summary>
	public decimal Weight5
	{
		get => _weight5.Value;
		set => _weight5.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (high, high, low) RSI combination.
	/// </summary>
	public decimal Weight6
	{
		get => _weight6.Value;
		set => _weight6.Value = value;
	}

	/// <summary>
	/// Neural network weight for the (high, high, high) RSI combination.
	/// </summary>
	public decimal Weight7
	{
		get => _weight7.Value;
		set => _weight7.Value = value;
	}

	/// <summary>
	/// Candle series used for indicator calculations and trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RnnProbabilityStrategy"/> class.
	/// </summary>
	public RnnProbabilityStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 9)
			.SetDisplay("RSI Period", "Length of the RSI indicator feeding the neural network.", "Indicator")
			.SetRange(2, 200)
			.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Open)
			.SetDisplay("Applied Price", "Price type forwarded to the RSI indicator.", "Indicator");

		_stopLossTakeProfitPips = Param(nameof(StopLossTakeProfitPips), 100m)
			.SetDisplay("Stop Loss & Take Profit (pips)", "Distance used for both stop-loss and take-profit levels.", "Risk")
			.SetRange(0m, 1000m)
			.SetCanOptimize(true);

		_weight0 = Param(nameof(Weight0), 6m)
			.SetDisplay("Weight 0", "Probability weight applied when all RSI inputs are low.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight1 = Param(nameof(Weight1), 96m)
			.SetDisplay("Weight 1", "Probability weight for the (low, low, high) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight2 = Param(nameof(Weight2), 90m)
			.SetDisplay("Weight 2", "Probability weight for the (low, high, low) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight3 = Param(nameof(Weight3), 35m)
			.SetDisplay("Weight 3", "Probability weight for the (low, high, high) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight4 = Param(nameof(Weight4), 64m)
			.SetDisplay("Weight 4", "Probability weight for the (high, low, low) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight5 = Param(nameof(Weight5), 83m)
			.SetDisplay("Weight 5", "Probability weight for the (high, low, high) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight6 = Param(nameof(Weight6), 66m)
			.SetDisplay("Weight 6", "Probability weight for the (high, high, low) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_weight7 = Param(nameof(Weight7), 50m)
			.SetDisplay("Weight 7", "Probability weight for the (high, high, high) branch.", "Model")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal generation.", "General");
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

		_rsiHistory.Clear();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pipSize = CalculatePipSize();

		Unit stopLossUnit = null;
		Unit takeProfitUnit = null;

		if (StopLossTakeProfitPips > 0m && _pipSize > 0m)
		{
			var distance = StopLossTakeProfitPips * _pipSize;
			stopLossUnit = new Unit(distance, UnitTypes.Absolute);
			takeProfitUnit = new Unit(distance, UnitTypes.Absolute);
		}

		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(
				takeProfit: takeProfitUnit,
				stopLoss: stopLossUnit,
				isStopTrailing: false,
				useMarketOrders: true);
		}

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_rsi == null)
			return;

		if (RsiPeriod <= 0)
			return;

		var price = GetPrice(candle, AppliedPrice);
		var rsiValue = _rsi.Process(price, candle.OpenTime, true).ToDecimal();

		if (!_rsi.IsFormed)
			return;

		_rsiHistory.Add(rsiValue);
		TrimHistory(_rsiHistory, GetHistoryLimit());

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var lastIndex = _rsiHistory.Count - 1;
		var delayedIndex = lastIndex - RsiPeriod;
		var delayedTwiceIndex = lastIndex - (2 * RsiPeriod);

		if (delayedIndex < 0 || delayedTwiceIndex < 0)
			return;

		var p1 = _rsiHistory[lastIndex] / 100m;
		var p2 = _rsiHistory[delayedIndex] / 100m;
		var p3 = _rsiHistory[delayedTwiceIndex] / 100m;

		var probability = CalculateProbability(p1, p2, p3);
		var signal = probability * 2m - 1m;

		LogInfo($"RSI inputs: p1={p1:F4}, p2={p2:F4}, p3={p3:F4}, probability={probability:F4}, signal={signal:F4}");

		if (TradeVolume <= 0m)
			return;

		if (Position != 0m)
			return;

		if (signal < 0m)
		{
			BuyMarket(TradeVolume);
		}
		else
		{
			SellMarket(TradeVolume);
		}
	}

	private decimal CalculateProbability(decimal p1, decimal p2, decimal p3)
	{
		var pn1 = 1m - p1;
		var pn2 = 1m - p2;
		var pn3 = 1m - p3;

		var probability =
			pn1 * (pn2 * (pn3 * Weight0 + p3 * Weight1) +
			        p2 * (pn3 * Weight2 + p3 * Weight3)) +
			p1 * (pn2 * (pn3 * Weight4 + p3 * Weight5) +
			        p2 * (pn3 * Weight6 + p3 * Weight7));

		return probability / 100m;
	}

	private int GetHistoryLimit()
	{
		return Math.Max((2 * RsiPeriod) + 5, RsiPeriod + 1);
	}

	private static void TrimHistory<T>(List<T> source, int maxSize)
	{
		if (maxSize <= 0)
			return;

		if (source.Count <= maxSize)
			return;

		var removeCount = source.Count - maxSize;
		source.RemoveRange(0, removeCount);
	}

	private decimal CalculatePipSize()
	{
		if (Security == null)
			return 0m;

		var step = Security.PriceStep ?? Security.Step ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}


using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that sells after a sequence of bullish candles followed by a bearish candle.
/// </summary>
public class NUp1DownStrategy : Strategy
{
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal Open, decimal Close)> _recentCandles = new();

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;

	/// <summary>
	/// Number of consecutive bullish bars required before the bearish setup candle.
	/// </summary>
	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing stop step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Risk percentage used to size the position.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NUp1DownStrategy"/> class.
	/// </summary>
	public NUp1DownStrategy()
	{
		Volume = 1m;

		_barsCount = Param(nameof(BarsCount), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bullish Bars", "Number of bullish bars before the down bar", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step (pips)", "Trailing step before adjusting stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portfolio risk percentage per trade", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candle analysis", "General");
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

		_recentCandles.Clear();
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		UpdateTrailingAndExits(candle);

		_recentCandles.Enqueue((candle.OpenPrice, candle.ClosePrice));
		while (_recentCandles.Count > BarsCount + 1)
			_recentCandles.Dequeue();

		if (_recentCandles.Count < BarsCount + 1)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candles = _recentCandles.ToArray();
		var last = candles[^1];

		if (last.Close >= last.Open)
			return;

		var isPattern = true;

		for (var i = 1; i <= BarsCount; i++)
		{
			var index = candles.Length - 1 - i;
			var bar = candles[index];

			if (bar.Close <= bar.Open)
			{
				isPattern = false;
				break;
			}

			if (i < BarsCount)
			{
				var prev = candles[index - 1];
				if (bar.Close <= prev.Close)
				{
					isPattern = false;
					break;
				}
			}
		}

		if (!isPattern)
			return;

		if (Position < 0)
			return;

		var tradeVolume = CalculateOrderVolume();
		if (tradeVolume <= 0m)
			return;

		var totalVolume = tradeVolume + Math.Max(Position, 0m);
		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);

		_entryPrice = candle.ClosePrice;
		_activeStopPrice = _entryPrice + StopLossPips * _pipSize;
		_activeTakePrice = _entryPrice - TakeProfitPips * _pipSize;

		LogInfo($"Short entry after {BarsCount} bullish bars at {_entryPrice:0.#####}");
	}

	private void UpdateTrailingAndExits(ICandleMessage candle)
	{
		if (Position < 0)
		{
			var volumeToClose = Math.Abs(Position);
			if (volumeToClose <= 0m)
				return;

			if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volumeToClose);
				LogInfo($"Short exit by stop-loss at {stop:0.#####}");
				ResetPositionState();
				return;
			}

			if (_activeTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volumeToClose);
				LogInfo($"Short exit by take-profit at {take:0.#####}");
				ResetPositionState();
				return;
			}

			if (_activeStopPrice is decimal trailingStop)
			{
				var trailingDistance = TrailingStopPips * _pipSize;
				var trailingStep = TrailingStepPips * _pipSize;

				if (trailingDistance <= 0m)
					return;

				var currentAsk = candle.ClosePrice;
				var newStopCandidate = currentAsk + trailingDistance;

				if (newStopCandidate + trailingStep < trailingStop)
				{
					_activeStopPrice = newStopCandidate;
					LogInfo($"Short trailing stop moved to {_activeStopPrice:0.#####}");
				}
			}
		}
		else if (Position == 0)
		{
			ResetPositionState();
		}
	}

	private decimal CalculatePipSize()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
		{
			var decimals = CountDecimalPlaces(step);
			return decimals is 3 or 5 ? step * 10m : step;
		}

		return 1m;
	}

	private static int CountDecimalPlaces(decimal value)
	{
		var text = value.ToString(CultureInfo.InvariantCulture);
		var separatorIndex = text.IndexOf('.');
		return separatorIndex >= 0 ? text.Length - separatorIndex - 1 : 0;
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = Volume;
		var stopDistance = StopLossPips * _pipSize;

		if (Portfolio == null || stopDistance <= 0m)
			return baseVolume;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return baseVolume;

		var capital = Portfolio.CurrentValue ?? 0m;
		if (capital <= 0m)
			return baseVolume;

		var riskAmount = capital * (RiskPercent / 100m);
		if (riskAmount <= 0m)
			return baseVolume;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return baseVolume;

		var riskPerUnit = steps * stepPrice;
		if (riskPerUnit <= 0m)
			return baseVolume;

		var volumeFromRisk = riskAmount / riskPerUnit;
		if (volumeFromRisk <= 0m)
			return baseVolume;

		return Math.Max(baseVolume, volumeFromRisk);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
	}
}

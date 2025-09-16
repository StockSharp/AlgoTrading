namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI plus Recursive Filter Trend Line (RFTL) strategy converted from the MetaTrader 5 expert advisor.
/// Detects RSI swing highs and lows, projects trend lines through them and combines the signals with the RFTL filter.
/// </summary>
public class RsiRftlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;

	private RelativeStrengthIndex _rsiIndicator = null!;
	private RftlIndicator _rftlIndicator = null!;

	private readonly List<decimal> _rsiHistory = new();
	private readonly List<decimal> _rftlHistory = new();
	private readonly List<decimal> _closeHistory = new();

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private const int MaxHistoryLength = 600;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RsiRftlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqual(0)
			.SetDisplay("Stop Loss (pips)", "Protective stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqual(0)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetGreaterOrEqual(0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetGreaterOrEqual(0)
			.SetDisplay("Trailing Step (pips)", "Additional pips required before trailing advances", "Risk");
	}

	/// <summary>
	/// Candle data type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop offset in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum pip improvement before trailing adjusts again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
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

		_rsiHistory.Clear();
		_rftlHistory.Clear();
		_closeHistory.Clear();
		ResetTrailing();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsiIndicator = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_rftlIndicator = new RftlIndicator();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsiIndicator, _rftlIndicator, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _rftlIndicator);

			var oscillatorArea = CreateChartArea();
			DrawIndicator(oscillatorArea, _rsiIndicator);

			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal rftlValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AddSeriesValue(_closeHistory, candle.ClosePrice);

		var closedByRisk = UpdateRiskManagement(candle);

		if (_rsiIndicator.IsFormed)
			AddSeriesValue(_rsiHistory, rsiValue);

		if (_rftlIndicator.IsFormed)
			AddSeriesValue(_rftlHistory, rftlValue);

		if (closedByRisk)
			return;

		if (!_rsiIndicator.IsFormed || !_rftlIndicator.IsFormed)
			return;

		if (_rsiHistory.Count < 4 || _rftlHistory.Count < 2 || _closeHistory.Count < 2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi0 = GetSeriesValue(_rsiHistory, 0);
		var rsi1 = GetSeriesValue(_rsiHistory, 1);
		var rsi2 = GetSeriesValue(_rsiHistory, 2);

		if (rsi0 > 70m && Position > 0m)
		{
			ClosePosition();
			ResetTrailing();
		}

		if (rsi0 < 30m && Position < 0m)
		{
			ClosePosition();
			ResetTrailing();
		}

		var count = Math.Min(500, _rsiHistory.Count);
		var bufferUp = new decimal[count];
		var bufferDown = new decimal[count];

		for (var i = 0; i <= count - 4; i++)
		{
			var prev = GetSeriesValue(_rsiHistory, i + 1);
			var current = GetSeriesValue(_rsiHistory, i + 2);
			var next = GetSeriesValue(_rsiHistory, i + 3);

			if (prev < current && current >= next)
				bufferUp[i + 2] = current;

			if (prev > current && current <= next)
				bufferDown[i + 2] = current;
		}

		decimal vol1 = 0m, vol2 = 0m, vol3 = 0m, vol4 = 0m;
		var pos1 = -1;
		var pos2 = -1;
		var pos3 = -1;
		var pos4 = -1;

		var topCount = 0;
		for (var i = 0; i < count; i++)
		{
			var value = bufferUp[i];
			if (value > 40m && topCount == 0)
			{
				vol1 = value;
				pos1 = i;
				topCount++;
			}
			else if (value > 60m && value > vol1 && topCount != 0)
			{
				vol2 = value;
				pos2 = i;
				topCount++;
				break;
			}
		}

		if (pos2 > 0)
		{
			for (var i = 0; i < pos2; i++)
			{
				var value = bufferDown[i];
				if (value != 0m && value < 40m)
				{
					vol1 = 0m;
					vol2 = 0m;
					break;
				}
			}
		}

		var bottomCount = 0;
		for (var i = 0; i < count; i++)
		{
			var value = bufferDown[i];
			if (value != 0m && value < 60m && bottomCount == 0)
			{
				vol3 = value;
				pos3 = i;
				bottomCount++;
			}
			else if (value != 0m && value < 40m && value < vol3 && bottomCount != 0)
			{
				vol4 = value;
				pos4 = i;
				bottomCount++;
				break;
			}
		}

		if (pos4 > 0)
		{
			for (var i = 0; i < pos4; i++)
			{
				var value = bufferUp[i];
				if (value != 0m && value > 60m)
				{
					vol3 = 0m;
					vol4 = 0m;
					break;
				}
			}
		}

		decimal volDw = 0m;
		decimal volDwPrev = 0m;
		if (vol3 != 0m && vol4 != 0m && pos3 >= 0 && pos4 > pos3)
		{
			var slope = (vol3 - vol4) / (decimal)(pos4 - pos3);
			volDw = vol3 + pos3 * slope;
			volDwPrev = vol3 + (pos3 - 1) * slope;
		}

		decimal volUp = 0m;
		decimal volUpPrev = 0m;
		if (vol1 != 0m && vol2 != 0m && pos1 >= 0 && pos2 > pos1)
		{
			var slope = (vol1 - vol2) / (decimal)(pos2 - pos1);
			volUp = vol1 + pos1 * slope;
			volUpPrev = vol1 + (pos1 - 1) * slope;
		}

		var rftlPrev = GetSeriesValue(_rftlHistory, 1);
		var closePrev = GetSeriesValue(_closeHistory, 1);

		var sellSignal = volDw != 0m
			&& rsi1 < volDw
			&& rsi2 > volDwPrev
			&& rftlPrev > closePrev
			&& rsi2 > 50m
			&& rsi0 > 47m
			&& pos2 >= 0
			&& pos4 >= 0
			&& pos2 > pos4;

		if (sellSignal && Position >= 0m && TradeVolume > 0m)
		{
			var target = -TradeVolume;
			var delta = target - Position;

			if (delta < 0m)
			{
				SellMarket(-delta);
				ResetTrailing();
				return;
			}
		}

		var buySignal = volUp != 0m
			&& rsi1 > volUp
			&& rsi2 < volUpPrev
			&& rftlPrev < closePrev
			&& rsi2 < 50m
			&& rsi0 < 55m
			&& pos4 >= 0
			&& pos2 >= 0
			&& pos4 > pos2;

		if (buySignal && Position <= 0m && TradeVolume > 0m)
		{
			var target = TradeVolume;
			var delta = target - Position;

			if (delta > 0m)
			{
				BuyMarket(delta);
				ResetTrailing();
			}
		}
	}

	private bool UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			if (StopLossPips > 0)
			{
				var stopOffset = GetPriceOffset(StopLossPips);
				if (stopOffset > 0m)
				{
					var stopPrice = entry - stopOffset;
					if (candle.LowPrice <= stopPrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TakeProfitPips > 0)
			{
				var takeOffset = GetPriceOffset(TakeProfitPips);
				if (takeOffset > 0m)
				{
					var takePrice = entry + takeOffset;
					if (candle.HighPrice >= takePrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TrailingStopPips > 0)
			{
				var trailingDistance = GetPriceOffset(TrailingStopPips);
				if (trailingDistance > 0m)
				{
					var trailingStep = GetPriceOffset(TrailingStepPips);
					var profit = candle.ClosePrice - entry;

					if (profit > trailingDistance + trailingStep)
					{
						var desiredStop = candle.ClosePrice - trailingDistance;
						var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

						if (_longTrailingStop is null || desiredStop - _longTrailingStop.Value >= minimalImprovement)
							_longTrailingStop = desiredStop;
					}

					if (_longTrailingStop is decimal trail && candle.LowPrice <= trail)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			if (StopLossPips > 0)
			{
				var stopOffset = GetPriceOffset(StopLossPips);
				if (stopOffset > 0m)
				{
					var stopPrice = entry + stopOffset;
					if (candle.HighPrice >= stopPrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TakeProfitPips > 0)
			{
				var takeOffset = GetPriceOffset(TakeProfitPips);
				if (takeOffset > 0m)
				{
					var takePrice = entry - takeOffset;
					if (candle.LowPrice <= takePrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TrailingStopPips > 0)
			{
				var trailingDistance = GetPriceOffset(TrailingStopPips);
				if (trailingDistance > 0m)
				{
					var trailingStep = GetPriceOffset(TrailingStepPips);
					var profit = entry - candle.ClosePrice;

					if (profit > trailingDistance + trailingStep)
					{
						var desiredStop = candle.ClosePrice + trailingDistance;
						var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

						if (_shortTrailingStop is null || _shortTrailingStop.Value - desiredStop >= minimalImprovement)
							_shortTrailingStop = desiredStop;
					}

					if (_shortTrailingStop is decimal trail && candle.HighPrice >= trail)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}
		else
		{
			ResetTrailing();
		}

		return false;
	}

	private static void AddSeriesValue(List<decimal> target, decimal value)
	{
		target.Add(value);
		if (target.Count > MaxHistoryLength)
			target.RemoveRange(0, target.Count - MaxHistoryLength);
	}

	private static decimal GetSeriesValue(List<decimal> values, int shift)
	{
		var index = values.Count - 1 - shift;
		return index >= 0 ? values[index] : 0m;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		return pips * step;
	}
}

/// <summary>
/// Recursive filter trend line indicator reproducing the MT5 RFTL buffer.
/// </summary>
public sealed class RftlIndicator : BaseIndicator<decimal>
{
	private static readonly decimal[] Coefficients =
	{
		-0.0025097319m, 0.0513007762m, 0.1142800493m, 0.1699342860m, 0.2025269304m,
		0.2025269304m, 0.1699342860m, 0.1142800493m, 0.0513007762m, -0.0025097319m,
		-0.0353166244m, -0.0433375629m, -0.0311244617m, -0.0088618137m, 0.0120580088m,
		0.0233183633m, 0.0221931304m, 0.0115769653m, -0.0022157966m, -0.0126536111m,
		-0.0157416029m, -0.0113395830m, -0.0025905610m, 0.0059521459m, 0.0105212252m,
		0.0096970755m, 0.0046585685m, -0.0017079230m, -0.0063513565m, -0.0074539350m,
		-0.0050439973m, -0.0007459678m, 0.0032271474m, 0.0051357867m, 0.0044454862m,
		0.0018784961m, -0.0011065767m, -0.0031162862m, -0.0033443253m, -0.0022163335m,
		0.0002573669m, 0.0003650790m, 0.0060440751m, 0.0018747783m
	};

	private readonly Queue<decimal> _prices = new();

	/// <summary>
	/// Optional shift used for visual alignment.
	/// </summary>
	public int Shift { get; set; }

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		_prices.Enqueue(candle.ClosePrice);

		var maxSize = Coefficients.Length + Math.Max(Shift, 0);
		while (_prices.Count > maxSize)
			_prices.Dequeue();

		if (_prices.Count < Coefficients.Length)
			return new DecimalIndicatorValue(this, default, input.Time);

		var prices = _prices.ToArray();
		decimal sum = 0m;

		for (var i = 0; i < Coefficients.Length; i++)
		{
			var priceIndex = prices.Length - 1 - i;
			if (priceIndex < 0)
				break;

			sum += prices[priceIndex] * Coefficients[i];
		}

		return new DecimalIndicatorValue(this, sum, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_prices.Clear();
	}
}

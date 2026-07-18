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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the "Last ZZ50" MetaTrader expert.
/// It reads the latest ZigZag pivots and enters at the midpoint of the last two legs.
/// </summary>
public class LastZz50Strategy : Strategy
{
	private readonly StrategyParam<decimal> _zigZagDeviation;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ZigZag _zigZag = null!;
	private readonly List<decimal> _pivots = new();
	private decimal _entryPrice;

	/// <summary>
	/// ZigZag deviation (0..1).
	/// </summary>
	public decimal ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type for the ZigZag.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LastZz50Strategy()
	{
		_zigZagDeviation = Param(nameof(ZigZagDeviation), 0.003m)
			.SetDisplay("ZigZag Deviation", "Percentage change threshold (0..1)", "ZigZag")
			.SetRange(0.000001m, 0.999999m);

		_stopLossPoints = Param(nameof(StopLossPoints), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pts)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 5)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pts)", "Target distance in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to evaluate the ZigZag", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pivots.Clear();
		_zigZag?.Reset();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_zigZag = new ZigZag
		{
			Deviation = ZigZagDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var step = Security?.PriceStep ?? 1m;
		var stopLoss = StopLossPoints > 0 ? new Unit(step * StopLossPoints, UnitTypes.Absolute) : (Unit)null;
		var takeProfit = TakeProfitPoints > 0 ? new Unit(step * TakeProfitPoints, UnitTypes.Absolute) : (Unit)null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit, stopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _zigZag);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var result = _zigZag.Process(new CandleIndicatorValue(_zigZag, candle));

		if (!_zigZag.IsFormed)
			return;

		// Store new pivot if zigzag returned a value
		if (result is ZigZagIndicatorValue zzVal && !zzVal.IsEmpty)
		{
			var pivotPrice = zzVal.ToDecimal();
			if (pivotPrice > 0)
			{
				// Update or add pivot
				if (_pivots.Count > 0 && Math.Abs(_pivots[^1] - pivotPrice) < (Security?.PriceStep ?? 0.01m))
				{
					_pivots[^1] = pivotPrice;
				}
				else
				{
					_pivots.Add(pivotPrice);
					if (_pivots.Count > 50)
						_pivots.RemoveAt(0);
				}
			}
		}

		if (_pivots.Count < 3)
			return;

		var priceA = _pivots[^1];
		var priceB = _pivots[^2];
		var priceC = _pivots[^3];
		var price = candle.ClosePrice;

		// Midpoint of BC leg
		var midBC = (priceB + priceC) / 2m;

		// Midpoint of AB leg
		var midAB = (priceA + priceB) / 2m;

		// If last pivot is a low (B < C means upswing), buy at midpoint
		// If last pivot is a high (B > C means downswing), sell at midpoint
		if (priceB < priceC)
		{
			// Upswing from B to C, expect continuation up
			// Buy if price pulls back to midpoint
			if (price <= midBC && Position <= 0)
			{
					BuyMarket();
				_entryPrice = price;
			}
		}
		else if (priceB > priceC)
		{
			// Downswing from B to C, expect continuation down
			// Sell if price rallies to midpoint
			if (price >= midBC && Position >= 0)
			{
					SellMarket();
				_entryPrice = price;
			}
		}
	}
}

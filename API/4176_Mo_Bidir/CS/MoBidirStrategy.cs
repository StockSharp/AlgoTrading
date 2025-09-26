namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Recreates the MO bidirectional expert advisor by opening a hedged pair on each completed candle.
/// </summary>
public class MoBidirStrategy : Strategy
{
	private sealed class HedgeLeg
	{
		public bool IsLong;
		public decimal TargetVolume;
		public Order EntryOrder;
		public decimal FilledVolume;
		public decimal EntryCost;
		public decimal EntryPrice;
		public decimal? StopPrice;
		public decimal? TakeProfitPrice;

		public bool IsFilled(decimal tolerance)
		{
			return FilledVolume >= TargetVolume - tolerance && FilledVolume > 0m;
		}
	}

	private const decimal VolumeTolerance = 0.00000001m;

	private readonly List<HedgeLeg> _legs = new();

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pointSize;

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public MoBidirStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Lot size used for each hedge leg.", "Trading")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 80)
			.SetDisplay("Stop Loss (points)", "Distance from the entry price to trigger an exit.", "Risk")
			.SetGreaterThanOrEqualTo(0);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 750)
			.SetDisplay("Take Profit (points)", "Target distance from the entry price.", "Risk")
			.SetGreaterThanOrEqualTo(0);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to detect completed bars.", "Data");
	}

	/// <summary>
	/// Order size applied to both sides of the hedge.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to detect new bars.
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

		_legs.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = ResolvePointSize();

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

		UpdateProtection(candle);

		if (_legs.Count > 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		Volume = volume;

		OpenHedgePair(volume);
	}

	private void UpdateProtection(ICandleMessage candle)
	{
		if (_legs.Count == 0)
			return;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (!leg.IsFilled(VolumeTolerance))
				continue;

			var stopTriggered = false;
			var takeTriggered = false;

			if (leg.IsLong)
			{
				if (leg.StopPrice is decimal stop && candle.LowPrice <= stop)
				{
					stopTriggered = true;
				}
				else if (leg.TakeProfitPrice is decimal take && candle.HighPrice >= take)
				{
					takeTriggered = true;
				}
			}
			else
			{
				if (leg.StopPrice is decimal stop && candle.HighPrice >= stop)
				{
					stopTriggered = true;
				}
				else if (leg.TakeProfitPrice is decimal take && candle.LowPrice <= take)
				{
					takeTriggered = true;
				}
			}

			if (!stopTriggered && !takeTriggered)
				continue;

			CloseLeg(leg);
			_legs.RemoveAt(i);
		}
	}

	private void OpenHedgePair(decimal volume)
	{
		var longOrder = BuyMarket(volume);
		if (longOrder != null)
		{
			// Track the long leg until its protective exits are hit.
			_legs.Add(new HedgeLeg
			{
				IsLong = true,
				TargetVolume = volume,
				EntryOrder = longOrder,
			});
		}

		var shortOrder = SellMarket(volume);
		if (shortOrder != null)
		{
			// Track the short leg until the stop or target is triggered.
			_legs.Add(new HedgeLeg
			{
				IsLong = false,
				TargetVolume = volume,
				EntryOrder = shortOrder,
			});
		}
	}

	private void CloseLeg(HedgeLeg leg)
	{
		var volume = leg.FilledVolume > 0m ? leg.FilledVolume : leg.TargetVolume;
		if (volume <= 0m)
			return;

		if (leg.IsLong)
		{
			// Exit the long leg with a market sell order.
			SellMarket(volume);
		}
		else
		{
			// Exit the short leg with a market buy order.
			BuyMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
			return;

		foreach (var leg in _legs)
		{
			if (order != leg.EntryOrder)
				continue;

			// Aggregate fills to compute the average entry price.
			leg.FilledVolume += trade.Volume;
			leg.EntryCost += trade.Price * trade.Volume;

			if (leg.FilledVolume > 0m)
				leg.EntryPrice = leg.EntryCost / leg.FilledVolume;

			if (leg.IsFilled(VolumeTolerance))
			{
				leg.EntryOrder = null;
				leg.StopPrice = CalculateStopPrice(leg);
				leg.TakeProfitPrice = CalculateTakeProfitPrice(leg);
			}

			break;
		}
	}

	private decimal? CalculateStopPrice(HedgeLeg leg)
	{
		if (StopLossPoints <= 0 || _pointSize <= 0m)
			return null;

		var offset = StopLossPoints * _pointSize;
		return leg.IsLong ? leg.EntryPrice - offset : leg.EntryPrice + offset;
	}

	private decimal? CalculateTakeProfitPrice(HedgeLeg leg)
	{
		if (TakeProfitPoints <= 0 || _pointSize <= 0m)
			return null;

		var offset = TakeProfitPoints * _pointSize;
		return leg.IsLong ? leg.EntryPrice + offset : leg.EntryPrice - offset;
	}

	private decimal ResolvePointSize()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
			return step;

		if (security?.MinPriceStep is decimal minStep && minStep > 0m)
			return minStep;

		return 0m;
	}
}

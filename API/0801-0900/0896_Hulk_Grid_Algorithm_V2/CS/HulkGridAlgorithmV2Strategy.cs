using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hulk Grid Algorithm V2 strategy.
/// Places ten buy limit orders at MidPrice +/- 1..5 GridStep (MidPrice 0 means the current close). An order k steps from the mid
/// has volume Lot * (6 - k), so orders closer to the mid are larger. When price touches StopLossPercent below the lowest level or
/// TakeProfitPercent above the highest level, the position is closed and the remaining orders are cancelled. With MidPrice 0
/// a new grid is then built around the current close.
/// </summary>
public class HulkGridAlgorithmV2Strategy : Strategy
{
	private const int _levelsPerSide = 5;

	private readonly StrategyParam<decimal> _midPrice;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<DataType> _candleType;

	private bool _gridActive;
	private bool _gridCompleted;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Grid mid price, 0 uses the current close.
	/// </summary>
	public decimal MidPrice
	{
		get => _midPrice.Value;
		set => _midPrice.Value = value;
	}

	/// <summary>
	/// Stop loss in percent below the lowest grid level.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit in percent above the highest grid level.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Price distance between grid levels.
	/// </summary>
	public decimal GridStep
	{
		get => _gridStep.Value;
		set => _gridStep.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal Lot
	{
		get => _lot.Value;
		set => _lot.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HulkGridAlgorithmV2Strategy()
	{
		_midPrice = Param(nameof(MidPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Mid Price", "Grid mid price, 0 uses the current close", "Grid");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss in percent below the lowest grid level", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit in percent above the highest grid level", "Risk");

		_gridStep = Param(nameof(GridStep), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Price distance between grid levels", "Grid");

		_lot = Param(nameof(Lot), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Lot", "Base order volume", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_gridActive = false;
		_gridCompleted = false;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_gridActive)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				CancelActiveOrders();

				if (Position > 0)
					SellMarket(Position);

				_gridActive = false;
				_gridCompleted = MidPrice > 0;
			}

			return;
		}

		if (_gridCompleted || Position != 0)
			return;

		PlaceGrid(MidPrice > 0 ? MidPrice : candle.ClosePrice);
	}

	private void PlaceGrid(decimal mid)
	{
		var step = Security.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var lowest = decimal.MaxValue;
		var highest = decimal.MinValue;

		for (var k = -_levelsPerSide; k <= _levelsPerSide; k++)
		{
			if (k == 0)
				continue;

			var price = Math.Round((mid + k * GridStep) / step) * step;
			if (price <= 0)
				continue;

			BuyLimit(price, Lot * (_levelsPerSide + 1 - Math.Abs(k)));

			lowest = Math.Min(lowest, price);
			highest = Math.Max(highest, price);
		}

		if (lowest == decimal.MaxValue)
			return;

		_stopPrice = lowest * (1m - StopLossPercent / 100m);
		_takePrice = highest * (1m + TakeProfitPercent / 100m);
		_gridActive = true;
	}
}

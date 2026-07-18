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
/// Places simulated symmetric stop entries at scheduled hours and manages them with daily resets.
/// </summary>
public class PendingOrdersByTimeStrategy : Strategy
{
	private readonly StrategyParam<int> _openingHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;
	private decimal? _entryPrice;

	public int OpeningHour
	{
		get => _openingHour.Value;
		set => _openingHour.Value = value;
	}

	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PendingOrdersByTimeStrategy()
	{
		_openingHour = Param(nameof(OpeningHour), 2)
			.SetDisplay("Opening Hour", "Hour to activate pending orders", "Schedule")
			.SetRange(0, 23);

		_closingHour = Param(nameof(ClosingHour), 22)
			.SetDisplay("Closing Hour", "Hour to cancel orders and flat positions", "Schedule")
			.SetRange(0, 23);

		_distancePips = Param(nameof(DistancePips), 500m)
			.SetDisplay("Distance (pips)", "Offset for entry stop orders", "Orders")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 2000m)
			.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe for the schedule", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_entryPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.01m;

		if (step <= 0m)
			return 0.01m;

		return step;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		// Check pending stop entries
		CheckPendingEntries(candle);

		// Manage existing position
		ManageRisk(candle);

		if (hour == ClosingHour)
		{
			// Closing hour: cancel pending and exit any open trades.
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
			ExitPosition();
		}

		if (hour == OpeningHour && hour != ClosingHour && Position == 0m && !_pendingBuyPrice.HasValue)
		{
			// Opening hour: set up new pending entries.
			SetupPendingEntries(candle.ClosePrice);
		}
	}

	private void CheckPendingEntries(ICandleMessage candle)
	{
		if (_pendingBuyPrice is decimal buyPrice && candle.HighPrice >= buyPrice && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = buyPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
			return;
		}

		if (_pendingSellPrice is decimal sellPrice && candle.LowPrice <= sellPrice && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = sellPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (_pipSize <= 0m || _entryPrice is not decimal entry)
			return;

		var takeProfitDistance = TakeProfitPips * _pipSize;
		var stopLossDistance = StopLossPips * _pipSize;

		if (Position > 0m)
		{
			if (takeProfitDistance > 0m && candle.HighPrice - entry >= takeProfitDistance)
			{
				SellMarket();
				_entryPrice = null;
				return;
			}

			if (stopLossDistance > 0m && entry - candle.LowPrice >= stopLossDistance)
			{
				SellMarket();
				_entryPrice = null;
			}
		}
		else if (Position < 0m)
		{
			if (takeProfitDistance > 0m && entry - candle.LowPrice >= takeProfitDistance)
			{
				BuyMarket();
				_entryPrice = null;
				return;
			}

			if (stopLossDistance > 0m && candle.HighPrice - entry >= stopLossDistance)
			{
				BuyMarket();
				_entryPrice = null;
			}
		}
	}

	private void ExitPosition()
	{
		if (Position > 0m)
			SellMarket();
		else if (Position < 0m)
			BuyMarket();
		_entryPrice = null;
	}

	private void SetupPendingEntries(decimal referencePrice)
	{
		if (_pipSize <= 0m)
			return;

		var distance = DistancePips * _pipSize;
		if (distance <= 0m)
			return;

		_pendingBuyPrice = referencePrice + distance;
		_pendingSellPrice = referencePrice - distance;
	}
}

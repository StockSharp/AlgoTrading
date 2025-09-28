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
/// Translation of the MetaTrader expert advisor "MySystem.mq4" based on Bulls Power and Bears Power momentum.
/// Combines the latest indicator values to decide entries and mirrors the trailing and bracket exit logic.
/// </summary>
public class MySystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _powerPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private BullPower? _bullsPower;
	private BearPower? _bearsPower;

	private decimal _pipSize;
	private decimal? _previousAveragePower;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop trigger distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for new entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Period applied to both Bulls Power and Bears Power indicators.
	/// </summary>
	public int PowerPeriod
	{
		get => _powerPeriod.Value;
		set => _powerPeriod.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters and optimization metadata.
	/// </summary>
	public MySystemStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 86m)
			.SetDisplay("Take Profit (points)", "Distance for take profit orders in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 60m)
			.SetDisplay("Stop Loss (points)", "Distance for stop loss orders in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetDisplay("Trailing Stop (points)", "Trailing exit trigger distance in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);

		_orderVolume = Param(nameof(OrderVolume), 8.3m)
			.SetDisplay("Order Volume", "Volume used when placing entries", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 10m, 0.1m);

		_powerPeriod = Param(nameof(PowerPeriod), 13)
			.SetDisplay("Power Period", "Indicator length for Bulls/Bears Power", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations", "General");
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

		_bullsPower = null;
		_bearsPower = null;
		_pipSize = 0m;
		_previousAveragePower = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bullsPower = new BullPower { Length = PowerPeriod };
		_bearsPower = new BearPower { Length = PowerPeriod };

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_bullsPower, _bearsPower, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bullsPower);
			DrawIndicator(area, _bearsPower);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			return;
		}

		var entryPrice = PositionPrice;
		var distance = _pipSize;

		if (Position > 0 && delta > 0)
		{
			// Store the initial protective levels for a fresh long position.
			_longStopPrice = StopLossPoints > 0m ? entryPrice - StopLossPoints * distance : (decimal?)null;
			_longTakePrice = TakeProfitPoints > 0m ? entryPrice + TakeProfitPoints * distance : (decimal?)null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0 && delta < 0)
		{
			// Store the initial protective levels for a fresh short position.
			_shortStopPrice = StopLossPoints > 0m ? entryPrice + StopLossPoints * distance : (decimal?)null;
			_shortTakePrice = TakeProfitPoints > 0m ? entryPrice - TakeProfitPoints * distance : (decimal?)null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsPowerValue, decimal bearsPowerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_bullsPower == null || _bearsPower == null)
			return;

		var previousAverage = _previousAveragePower;

		var exitTriggered = false;
		if (Position > 0)
		{
			exitTriggered = ManageLongPosition(candle, previousAverage, bullsPowerValue, bearsPowerValue);
		}
		else if (Position < 0)
		{
			exitTriggered = ManageShortPosition(candle, bullsPowerValue, bearsPowerValue);
		}

		if (!_bullsPower.IsFormed || !_bearsPower.IsFormed)
			return;

		var currentAverage = (bullsPowerValue + bearsPowerValue) / 2m;
		_previousAveragePower = currentAverage;

		if (!IsFormedAndOnlineAndAllowTrading() || Position != 0 || exitTriggered)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		if (previousAverage.HasValue && previousAverage.Value > currentAverage && currentAverage > 0m)
		{
			// Momentum flipped from bullish to weaker positive territory -> open a short trade.
			SellMarket(volume);
			return;
		}

		if (currentAverage < 0m)
		{
			// Bears dominate and the combined power turns negative -> open a long trade.
			BuyMarket(volume);
		}
	}

	private bool ManageLongPosition(ICandleMessage candle, decimal? previousAverage, decimal bullsPowerValue, decimal bearsPowerValue)
	{
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			// Target reached for the long position.
			ClosePosition();
			return true;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			// Protective stop triggered for the long position.
			ClosePosition();
			return true;
		}

		if (TrailingStopPoints <= 0m || !previousAverage.HasValue)
			return false;

		var currentAverage = (bullsPowerValue + bearsPowerValue) / 2m;
		if (previousAverage.Value > currentAverage)
		{
			var distance = TrailingStopPoints * _pipSize;
			if (distance > 0m && candle.HighPrice - PositionPrice >= distance)
			{
				// Price advanced enough and the average power weakened -> trail out of the long trade.
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private bool ManageShortPosition(ICandleMessage candle, decimal bullsPowerValue, decimal bearsPowerValue)
	{
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			// Target reached for the short position.
			ClosePosition();
			return true;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			// Protective stop triggered for the short position.
			ClosePosition();
			return true;
		}

		if (TrailingStopPoints <= 0m)
			return false;

		var currentAverage = (bullsPowerValue + bearsPowerValue) / 2m;
		if (currentAverage < 0m)
		{
			var distance = TrailingStopPoints * _pipSize;
			if (distance > 0m && PositionPrice - candle.LowPrice >= distance)
			{
				// Momentum flipped to bearish and price dropped sufficiently -> trail out of the short trade.
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}

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
/// Trading panel strategy that enters positions using configurable offset distances
/// and manages them with stop-loss and take-profit levels.
/// Simplified from the CM Panel MetaTrader script.
/// </summary>
public class CmPanelStrategy : Strategy
{
	private readonly StrategyParam<int> _buyOffsetPoints;
	private readonly StrategyParam<int> _sellOffsetPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _priceStep;

	/// <summary>
	/// Buy trigger offset in points above SMA.
	/// </summary>
	public int BuyOffsetPoints
	{
		get => _buyOffsetPoints.Value;
		set => _buyOffsetPoints.Value = value;
	}

	/// <summary>
	/// Sell trigger offset in points below SMA.
	/// </summary>
	public int SellOffsetPoints
	{
		get => _sellOffsetPoints.Value;
		set => _sellOffsetPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CmPanelStrategy()
	{
		_buyOffsetPoints = Param(nameof(BuyOffsetPoints), 100)
			.SetNotNegative()
			.SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances");

		_sellOffsetPoints = Param(nameof(SellOffsetPoints), 100)
			.SetNotNegative()
			.SetDisplay("Sell Offset", "Distance below SMA for sell entry (points)", "Distances");

		_stopLossPoints = Param(nameof(StopLossPoints), 100)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for signals", "General");
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
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceStep = Security?.PriceStep ?? 0.01m;

		var sma = new SimpleMovingAverage { Length = 20 };

		SubscribeCandles(CandleType)
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var price = candle.ClosePrice;
		var step = _priceStep > 0m ? _priceStep : 0.01m;

		// Check stop-loss / take-profit for open positions
		if (Position != 0 && _entryPrice > 0m)
		{
			if (Position > 0)
			{
				if (_stopPrice.HasValue && price <= _stopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
				if (_takePrice.HasValue && price >= _takePrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
			else if (Position < 0)
			{
				if (_stopPrice.HasValue && price >= _stopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
				if (_takePrice.HasValue && price <= _takePrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
		}

		// Entry: price crosses above SMA + offset => buy, below SMA - offset => sell
		if (Position == 0)
		{
			var buyLevel = smaValue + BuyOffsetPoints * step;
			var sellLevel = smaValue - SellOffsetPoints * step;

			if (price >= buyLevel)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = StopLossPoints > 0 ? price - StopLossPoints * step : null;
				_takePrice = TakeProfitPoints > 0 ? price + TakeProfitPoints * step : null;
			}
			else if (price <= sellLevel)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = StopLossPoints > 0 ? price + StopLossPoints * step : null;
				_takePrice = TakeProfitPoints > 0 ? price - TakeProfitPoints * step : null;
			}
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}
}

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
/// Strategy replicating the logic of the original MetaTrader TST expert advisor.
/// Buys after a deep pullback from the session high and sells after a rally from the session low.
/// Applies static stop-loss and take-profit levels measured in price steps.
/// </summary>
public class TstStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _gapPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal _priceStep;
	private DateTimeOffset? _lastSignalBarTime;

	/// <summary>
	/// Stop-loss distance expressed in security price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in security price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance between the extreme price and the close required to issue a reversal signal.
	/// </summary>
	public int GapPoints
	{
		get => _gapPoints.Value;
		set => _gapPoints.Value = value;
	}

	/// <summary>
	/// Order volume used when opening new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculating trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TstStrategy"/> class.
	/// </summary>
	public TstStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 500)
			.SetDisplay("Stop Loss Points", "Stop-loss distance in price steps", "Risk Management")
			.SetRange(0, 100000);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetDisplay("Take Profit Points", "Take-profit distance in price steps", "Risk Management")
			.SetRange(0, 100000);

		_gapPoints = Param(nameof(GapPoints), 500)
			.SetDisplay("Pullback Points", "Minimal pullback from the high or low in price steps", "Signals")
			.SetRange(0, 100000);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume to trade on each signal", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyse", "General");
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

		_priceStep = 0m;
		_lastSignalBarTime = null;
		ResetRiskLevels();

		Volume = OrderVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to stay in sync with the Designer workflow.
		if (candle.State != CandleStates.Finished)
			return;

		// Exit an existing position before reacting to fresh entry signals.
		if (ManageRisk(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lastSignalBarTime == candle.OpenTime)
			return;

		var threshold = GetPriceOffset(GapPoints);
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// Enter long when the candle closed well below its open after printing a significant high.
		if (Position <= 0 && open > close && high - close > threshold)
		{
			var volume = OrderVolume + (Position < 0 ? Math.Abs(Position) : 0m);

			if (volume > 0m)
			{
				BuyMarket(volume);

				var entryPrice = close;

				_longStopPrice = StopLossPoints > 0 ? entryPrice - GetPriceOffset(StopLossPoints) : null;
				_longTakePrice = TakeProfitPoints > 0 ? entryPrice + GetPriceOffset(TakeProfitPoints) : null;
				_shortStopPrice = null;
				_shortTakePrice = null;

				_lastSignalBarTime = candle.OpenTime;

				LogInfo($"Entered long at {entryPrice}.");
			}

			return;
		}

		// Enter short when the candle closed well above its open after probing a deep low.
		if (Position >= 0 && close > open && close - low > threshold)
		{
			var closingVolume = Position > 0 ? Position : 0m;
			var volume = OrderVolume + closingVolume;

			if (volume > 0m)
			{
				SellMarket(volume);

				var entryPrice = close;

				_shortStopPrice = StopLossPoints > 0 ? entryPrice + GetPriceOffset(StopLossPoints) : null;
				_shortTakePrice = TakeProfitPoints > 0 ? entryPrice - GetPriceOffset(TakeProfitPoints) : null;
				_longStopPrice = null;
				_longTakePrice = null;

				_lastSignalBarTime = candle.OpenTime;

				LogInfo($"Entered short at {entryPrice}.");
			}
		}
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Position;

			// Stop-loss exits take priority for long trades.
			if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				SellMarket(volume);
				ResetRiskLevels();
				LogInfo($"Long stop hit at {stopPrice}.");
				return true;
			}

			// Take-profit exit for long trades.
			if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
			{
				SellMarket(volume);
				ResetRiskLevels();
				LogInfo($"Long take-profit hit at {takePrice}.");
				return true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			// Stop-loss exits take priority for short trades.
			if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				BuyMarket(volume);
				ResetRiskLevels();
				LogInfo($"Short stop hit at {stopPrice}.");
				return true;
			}

			// Take-profit exit for short trades.
			if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
			{
				BuyMarket(volume);
				ResetRiskLevels();
				LogInfo($"Short take-profit hit at {takePrice}.");
				return true;
			}
		}
		else
		{
			ResetRiskLevels();
		}

		return false;
	}

	private decimal GetPriceOffset(int points)
	{
		if (points <= 0)
			return 0m;

		return points * (_priceStep == 0m ? 1m : _priceStep);
	}

	private void ResetRiskLevels()
	{
		// Clear stored stop and target levels once the position is closed.
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}
}

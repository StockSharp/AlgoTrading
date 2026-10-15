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
/// Price monitoring strategy that logs OHLC metrics and trades on candle patterns.
/// Simplified from the "Symbol Swap Panel" MQL display widget.
/// </summary>
public class SymbolSwapPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private SimpleMovingAverage _sma;
	private decimal _entryPrice;
	private decimal _prevClose;

	/// <summary>
	/// Candle type for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for trend signals.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SymbolSwapPanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for monitoring and signals", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for entry signals", "Indicators");
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
		_sma = null;
		_entryPrice = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// Log price info
		LogInfo(
			$"Time: {candle.CloseTime:O}, O: {candle.OpenPrice}, H: {high}, L: {low}, C: {price}, " +
			$"Vol: {candle.TotalVolume}, SMA: {smaValue:F5}");

		// Exit: reversal or profit target
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			// Exit on trend reversal
			if ((Position > 0 && price < smaValue) ||
				(Position < 0 && price > smaValue))
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				_prevClose = price;
				return;
			}
		}

		// Entry: follow MA trend with momentum confirmation
		if (Position == 0 && _prevClose > 0m)
		{
			if (price > smaValue && _prevClose <= smaValue)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (price < smaValue && _prevClose >= smaValue)
			{
				SellMarket();
				_entryPrice = price;
			}
		}

		_prevClose = price;
	}
}

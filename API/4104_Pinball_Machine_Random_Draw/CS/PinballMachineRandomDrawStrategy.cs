using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pinball Machine: Pseudo-random entry with ATR-based risk management.
/// Uses candle hash to generate deterministic random signals.
/// </summary>
public class PinballMachineRandomDrawStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private int _candleCount;

	public PinballMachineRandomDrawStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
		_candleCount = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_candleCount = 0;

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		_candleCount++;
		var close = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 1.5m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 1.5m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Pseudo-random entry based on candle price hash
		if (Position == 0)
		{
			var hash = (int)(close * 100m) ^ _candleCount;
			var mod = Math.Abs(hash) % 10;

			if (mod < 3)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (mod > 6)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}

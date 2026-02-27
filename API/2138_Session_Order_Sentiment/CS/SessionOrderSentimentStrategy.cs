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
/// Strategy trading on volume sentiment using candle data.
/// Compares bullish vs bearish volume over a lookback period.
/// </summary>
public class SessionOrderSentimentStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeRatio;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal vol, bool isBull)> _volumeHistory = new();
	private decimal _entryPrice;

	/// <summary>
	/// Volume ratio required for entry.
	/// </summary>
	public decimal VolumeRatio
	{
		get => _volumeRatio.Value;
		set => _volumeRatio.Value = value;
	}

	/// <summary>
	/// Lookback period in candles.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SessionOrderSentimentStrategy"/>.
	/// </summary>
	public SessionOrderSentimentStrategy()
	{
		_volumeRatio = Param(nameof(VolumeRatio), 1.5m)
			.SetDisplay("Volume Ratio", "Bull/bear volume ratio for entry", "General")
			.SetGreaterThanZero();

		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Number of candles to look back", "General")
			.SetGreaterThanZero();

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var isBull = candle.ClosePrice >= candle.OpenPrice;
		_volumeHistory.Add((candle.TotalVolume, isBull));

		if (_volumeHistory.Count > Lookback)
			_volumeHistory.RemoveAt(0);

		if (_volumeHistory.Count < Lookback)
			return;

		var bullVolume = 0m;
		var bearVolume = 0m;

		foreach (var (vol, bull) in _volumeHistory)
		{
			if (bull)
				bullVolume += vol;
			else
				bearVolume += vol;
		}

		if (bearVolume == 0) bearVolume = 1;
		if (bullVolume == 0) bullVolume = 1;

		var bullBearRatio = bullVolume / bearVolume;
		var bearBullRatio = bearVolume / bullVolume;

		var close = candle.ClosePrice;

		// Check stop loss
		if (Position > 0 && close <= _entryPrice * (1m - StopLossPct / 100m))
		{
			SellMarket();
			return;
		}
		if (Position < 0 && close >= _entryPrice * (1m + StopLossPct / 100m))
		{
			BuyMarket();
			return;
		}

		// Bullish sentiment
		if (bullBearRatio >= VolumeRatio)
		{
			if (Position < 0)
			{
				BuyMarket();
			}
			if (Position <= 0)
			{
				_entryPrice = close;
				BuyMarket();
			}
		}
		// Bearish sentiment
		else if (bearBullRatio >= VolumeRatio)
		{
			if (Position > 0)
			{
				SellMarket();
			}
			if (Position >= 0)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_volumeHistory.Clear();
		_entryPrice = 0m;
	}
}

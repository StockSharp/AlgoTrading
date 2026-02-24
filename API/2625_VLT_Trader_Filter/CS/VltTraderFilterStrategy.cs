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
/// Volatility contraction breakout strategy converted from the VLT_TRADER MQL version.
/// Enters when the latest candle range is the smallest within recent history and
/// price breaks above/below the previous candle high/low.
/// </summary>
public class VltTraderFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _candleCount;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _rangeHistory = new();
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevRange;
	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Number of historical candles used for the volatility filter.
	/// </summary>
	public int CandleCount
	{
		get => _candleCount.Value;
		set => _candleCount.Value = value;
	}

	/// <summary>
	/// Take profit as a multiplier of the narrow range candle.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss as a multiplier of the narrow range candle.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VltTraderFilterStrategy"/> class.
	/// </summary>
	public VltTraderFilterStrategy()
	{
		_candleCount = Param(nameof(CandleCount), 6)
			.SetGreaterThanZero()
			.SetDisplay("Candle Count", "Number of historical candles used for the volatility filter", "Signals")
			.SetOptimize(3, 15, 1);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("TP Multiplier", "Take profit as multiplier of narrow range", "Risk")
			.SetOptimize(1m, 5m, 0.5m);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "Stop loss as multiplier of narrow range", "Risk")
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build signal candles", "General");
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

		_rangeHistory.Clear();
		_prevHigh = null;
		_prevLow = null;
		_prevRange = null;
		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var range = high - low;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(range, high, low);
			return;
		}

		// Check exit conditions for existing position
		if (Position != 0 && _entryPrice != 0 && _prevRange is decimal narrowRange && narrowRange > 0)
		{
			var tp = narrowRange * TakeProfitMultiplier;
			var sl = narrowRange * StopLossMultiplier;

			if (_isLong && Position > 0)
			{
				if (close >= _entryPrice + tp || close <= _entryPrice - sl)
				{
					SellMarket(Math.Abs(Position));
					UpdateHistory(range, high, low);
					return;
				}
			}
			else if (!_isLong && Position < 0)
			{
				if (close <= _entryPrice - tp || close >= _entryPrice + sl)
				{
					BuyMarket(Math.Abs(Position));
					UpdateHistory(range, high, low);
					return;
				}
			}
		}

		// Check entry conditions only when flat
		if (Position == 0 && _prevHigh.HasValue && _prevLow.HasValue && _prevRange.HasValue)
		{
			var prevH = _prevHigh.Value;
			var prevL = _prevLow.Value;
			var prevR = _prevRange.Value;

			if (prevR > 0 && _rangeHistory.Count >= CandleCount)
			{
				// Check if previous candle range was the narrowest
				var isNarrowest = true;
				foreach (var histRange in _rangeHistory)
				{
					if (histRange > 0 && histRange <= prevR)
					{
						isNarrowest = false;
						break;
					}
				}

				if (isNarrowest)
				{
					// Breakout detection on current candle
					if (close > prevH)
					{
						var volume = Volume;
						if (volume > 0)
						{
							BuyMarket(volume);
							_entryPrice = close;
							_isLong = true;
						}
					}
					else if (close < prevL)
					{
						var volume = Volume;
						if (volume > 0)
						{
							SellMarket(volume);
							_entryPrice = close;
							_isLong = false;
						}
					}
				}
			}
		}

		UpdateHistory(range, high, low);
	}

	private void UpdateHistory(decimal range, decimal high, decimal low)
	{
		if (_prevRange.HasValue)
		{
			_rangeHistory.Enqueue(_prevRange.Value);
			while (_rangeHistory.Count > CandleCount)
				_rangeHistory.Dequeue();
		}

		_prevRange = range;
		_prevHigh = high;
		_prevLow = low;
	}
}

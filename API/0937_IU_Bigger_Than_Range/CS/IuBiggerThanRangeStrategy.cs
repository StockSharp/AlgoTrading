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
/// Breakout strategy that enters when the candle body exceeds the previous range.
/// </summary>
public class IuBiggerThanRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _riskToReward;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRangeSize;
	private decimal _prevCandleHigh;
	private decimal _prevCandleLow;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _entryPrice;
	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;

	/// <summary>
	/// Lookback period for range calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public int RiskToReward
	{
		get => _riskToReward.Value;
		set => _riskToReward.Value = value;
	}

	/// <summary>
	/// ATR multiplier factor.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IuBiggerThanRangeStrategy"/> class.
	/// </summary>
	public IuBiggerThanRangeStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 22)
			.SetDisplay("Lookback Period", "Length for range calculation.", "Parameters");

		_riskToReward = Param(nameof(RiskToReward), 3)
			.SetDisplay("Risk To Reward", "Risk to reward ratio.", "Parameters");

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetDisplay("ATR Factor", "ATR multiplier.", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRangeSize = 0m;
		_prevCandleHigh = 0m;
		_prevCandleLow = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_entryPrice = 0m;
		_highestHigh = 0m;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;

		var atr = new AverageTrueRange { Length = 14 };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		// Track highest/lowest manually
		if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
		if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;

		var rangeSize = _highestHigh - _lowestLow;
		var candleBody = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (_barCount < LookbackPeriod)
		{
			_prevRangeSize = rangeSize;
			_prevCandleHigh = candle.HighPrice;
			_prevCandleLow = candle.LowPrice;
			return;
		}

		// Exit logic first
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.ClosePrice >= _targetPrice)
			{
				SellMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.ClosePrice <= _targetPrice)
			{
				BuyMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
				_entryPrice = 0m;
			}
		}

		// Entry logic
		if (Position == 0 && candleBody > _prevRangeSize * 0.5m)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - atrValue * AtrFactor;
				_targetPrice = _entryPrice + (_entryPrice - _stopPrice) * RiskToReward;
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + atrValue * AtrFactor;
				_targetPrice = _entryPrice - (_stopPrice - _entryPrice) * RiskToReward;
			}
		}

		_prevRangeSize = rangeSize;
		_prevCandleHigh = candle.HighPrice;
		_prevCandleLow = candle.LowPrice;
	}
}

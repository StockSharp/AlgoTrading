using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TrainYourself: Donchian channel breakout strategy.
/// Uses Highest/Lowest as channel, arms after price is inside channel,
/// then trades breakouts.
/// </summary>
public class TrainYourselfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private bool _isArmed;
	private decimal _entryPrice;

	public TrainYourselfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetDisplay("Channel Length", "Highest/Lowest period.", "Channel");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR for stop distance.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHighest = 0;
		_prevLowest = 0;
		_isArmed = false;
		_entryPrice = 0;

		var highest = new Highest { Length = ChannelLength };
		var lowest = new Lowest { Length = ChannelLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highVal, decimal lowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHighest == 0 || _prevLowest == 0 || atrVal <= 0)
		{
			_prevHighest = highVal;
			_prevLowest = lowVal;
			return;
		}

		var close = candle.ClosePrice;
		var upper = _prevHighest;
		var lower = _prevLowest;

		// Manage position: exit on channel re-entry
		if (Position > 0)
		{
			if (close < (upper + lower) / 2m)
			{
				SellMarket();
				_entryPrice = 0;
				_isArmed = false;
			}
		}
		else if (Position < 0)
		{
			if (close > (upper + lower) / 2m)
			{
				BuyMarket();
				_entryPrice = 0;
				_isArmed = false;
			}
		}

		// Arm when price is inside channel
		if (Position == 0)
		{
			if (!_isArmed)
			{
				var margin = atrVal * 0.2m;
				if (close > lower + margin && close < upper - margin)
					_isArmed = true;
			}
			else
			{
				// Breakout entry
				if (close > upper)
				{
					_entryPrice = close;
					BuyMarket();
					_isArmed = false;
				}
				else if (close < lower)
				{
					_entryPrice = close;
					SellMarket();
					_isArmed = false;
				}
			}
		}

		_prevHighest = highVal;
		_prevLowest = lowVal;
	}
}

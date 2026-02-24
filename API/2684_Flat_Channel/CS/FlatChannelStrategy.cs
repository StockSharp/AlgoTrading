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
/// Flat channel breakout strategy converted from the MetaTrader 5 version.
/// Detects consolidation via falling standard deviation, then trades breakouts of the channel.
/// </summary>
public class FlatChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<int> _flatBars;
	private readonly StrategyParam<decimal> _channelMinPips;
	private readonly StrategyParam<decimal> _channelMaxPips;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev = null!;
	private DonchianChannels _donchian = null!;

	private decimal _previousStdDev;
	private int _flatBarCount;
	private decimal _channelHigh;
	private decimal _channelLow;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;
	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Standard deviation indicator period.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Minimum number of bars with falling volatility required to form a flat channel.
	/// </summary>
	public int FlatBars
	{
		get => _flatBars.Value;
		set => _flatBars.Value = value;
	}

	/// <summary>
	/// Minimum channel width expressed in pips.
	/// </summary>
	public decimal ChannelMinPips
	{
		get => _channelMinPips.Value;
		set => _channelMinPips.Value = value;
	}

	/// <summary>
	/// Maximum channel width expressed in pips.
	/// </summary>
	public decimal ChannelMaxPips
	{
		get => _channelMaxPips.Value;
		set => _channelMaxPips.Value = value;
	}

	/// <summary>
	/// Candle type to analyse.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FlatChannelStrategy()
	{
		_stdDevPeriod = Param(nameof(StdDevPeriod), 37)
			.SetDisplay("StdDev Period", "Standard deviation indicator period", "Indicators")
			.SetGreaterThanZero();

		_flatBars = Param(nameof(FlatBars), 2)
			.SetDisplay("Flat Bars", "Minimum bars in flat state", "Indicators")
			.SetGreaterThanZero();

		_channelMinPips = Param(nameof(ChannelMinPips), 10m)
			.SetDisplay("Channel Min Pips", "Minimum channel width in pips", "Indicators")
			.SetGreaterThanZero();

		_channelMaxPips = Param(nameof(ChannelMaxPips), 100000m)
			.SetDisplay("Channel Max Pips", "Maximum channel width in pips", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousStdDev = 0m;
		_flatBarCount = 0;
		_channelHigh = 0m;
		_channelLow = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stdDev = new StandardDeviation { Length = StdDevPeriod };
		_donchian = new DonchianChannels { Length = FlatBars };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var stdDevValue = _stdDev.Process(new DecimalIndicatorValue(_stdDev, medianPrice, candle.CloseTime) { IsFinal = true }).ToDecimal();

		if (!_stdDev.IsFormed || channelValue is not DonchianChannelsValue donchianValue)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		if (donchianValue.UpperBand is not decimal upper || donchianValue.LowerBand is not decimal lower)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		// Update flat state based on StdDev direction.
		UpdateStdDevState(stdDevValue, upper, lower, candle);

		// Check simulated pending entries.
		CheckPendingEntries(candle);

		// Manage existing positions with SL/TP.
		ManagePosition(candle);

		// If flat and no position, set up pending breakout entries.
		if (Position == 0 && _flatBarCount >= FlatBars && _channelHigh > _channelLow)
		{
			var channelWidth = _channelHigh - _channelLow;
			var priceStep = Security?.PriceStep ?? 0.01m;
			if (priceStep <= 0m) priceStep = 0.01m;
			var minWidth = ChannelMinPips * priceStep;
			var maxWidth = ChannelMaxPips * priceStep;

			if (channelWidth >= minWidth && channelWidth <= maxWidth)
			{
				// Set pending breakout entries at channel boundaries.
				_pendingBuyPrice = _channelHigh;
				_pendingSellPrice = _channelLow;
				_longStop = _channelHigh - channelWidth * 2m;
				_longTake = _channelHigh + channelWidth;
				_shortStop = _channelLow + channelWidth * 2m;
				_shortTake = _channelLow - channelWidth;
			}
		}

		_previousStdDev = stdDevValue;
	}

	private void UpdateStdDevState(decimal stdDevValue, decimal upper, decimal lower, ICandleMessage candle)
	{
		if (_previousStdDev == 0m)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		if (stdDevValue < _previousStdDev)
		{
			_flatBarCount++;

			if (_flatBarCount == FlatBars)
			{
				_channelHigh = upper;
				_channelLow = lower;
			}
			else if (_flatBarCount > FlatBars)
			{
				if (candle.HighPrice > _channelHigh)
					_channelHigh = candle.HighPrice;
				if (candle.LowPrice < _channelLow)
					_channelLow = candle.LowPrice;
			}
		}
		else if (stdDevValue > _previousStdDev)
		{
			_flatBarCount = 0;
			_channelHigh = 0m;
			_channelLow = 0m;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
		else if (_flatBarCount >= FlatBars && _channelHigh <= _channelLow)
		{
			_channelHigh = upper;
			_channelLow = lower;
		}
	}

	private void CheckPendingEntries(ICandleMessage candle)
	{
		if (Position != 0)
			return;

		if (_pendingBuyPrice is decimal buyPrice && candle.HighPrice >= buyPrice)
		{
			BuyMarket(Volume);
			_entryPrice = buyPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
			return;
		}

		if (_pendingSellPrice is decimal sellPrice && candle.LowPrice <= sellPrice)
		{
			SellMarket(Volume);
			_entryPrice = sellPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop > 0m && candle.LowPrice <= _longStop)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
			if (_longTake > 0m && candle.HighPrice >= _longTake)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			if (_shortStop > 0m && candle.HighPrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
			if (_shortTake > 0m && candle.LowPrice <= _shortTake)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
	}
}

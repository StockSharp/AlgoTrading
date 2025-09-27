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
/// Channel-based mean reversion strategy converted from the Daydream expert advisor.
/// Buys when price closes below the recent Donchian lower band and shorts when price closes above the upper band.
/// Utilizes a virtual take profit expressed in pips and restricts entries to one per candle.
/// </summary>
public class DaydreamChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private DateTimeOffset _lastEntryCandleTime;
	private decimal _pipSize;

	/// <summary>
	/// Trading volume for each new entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Virtual take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Number of completed candles used to build the Donchian channel.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
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
	/// Initializes <see cref="DaydreamChannelBreakoutStrategy"/>.
	/// </summary>
	public DaydreamChannelBreakoutStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Lot size used for entries", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Virtual take profit distance in pips", "Risk Management")
			.SetCanOptimize(true);

		_channelPeriod = Param(nameof(ChannelPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Number of completed candles for channel calculation", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to subscribe", "General");

		_lastEntryCandleTime = DateTimeOffset.MinValue;
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

		_previousUpperBand = null;
		_previousLowerBand = null;
		_lastEntryCandleTime = DateTimeOffset.MinValue;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var donchianValue = (DonchianChannelsValue)indicatorValue;

		if (donchianValue.UpperBand is not decimal upperBand ||
			donchianValue.LowerBand is not decimal lowerBand)
		{
			return;
		}

		var takeProfitDistance = TakeProfitPips * (_pipSize != 0m ? _pipSize : CalculatePipSize());

		if (TryCloseForTakeProfit(candle.ClosePrice, takeProfitDistance))
		{
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			return;
		}

		if (_previousUpperBand is null || _previousLowerBand is null)
		{
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			return;
		}

		var previousUpper = _previousUpperBand.Value;
		var previousLower = _previousLowerBand.Value;

		if (candle.OpenTime > _lastEntryCandleTime)
		{
			if (candle.ClosePrice < previousLower)
			{
				var volume = OrderVolume + Math.Max(0m, -Position);

				if (volume > 0m)
				{
					BuyMarket(volume);
					LogInfo($"Long entry: close {candle.ClosePrice} dipped below channel low {previousLower}.");
					_lastEntryCandleTime = candle.OpenTime;
				}
			}
			else if (candle.ClosePrice > previousUpper)
			{
				var volume = OrderVolume + Math.Max(0m, Position);

				if (volume > 0m)
				{
					SellMarket(volume);
					LogInfo($"Short entry: close {candle.ClosePrice} climbed above channel high {previousUpper}.");
					_lastEntryCandleTime = candle.OpenTime;
				}
			}
		}

		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
	}

	private bool TryCloseForTakeProfit(decimal closePrice, decimal takeProfitDistance)
	{
		if (takeProfitDistance <= 0m || Position == 0)
			return false;

		if (Position > 0)
		{
			var entryPrice = PositionPrice;

			if (closePrice - entryPrice >= takeProfitDistance)
			{
				ClosePosition();
				LogInfo($"Virtual take profit triggered for long position. Entry {entryPrice}, close {closePrice}, distance {takeProfitDistance}.");
				return true;
			}
		}
		else
		{
			var entryPrice = PositionPrice;

			if (entryPrice - closePrice >= takeProfitDistance)
			{
				ClosePosition();
				LogInfo($"Virtual take profit triggered for short position. Entry {entryPrice}, close {closePrice}, distance {takeProfitDistance}.");
				return true;
			}
		}

		return false;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = CountDecimals(priceStep);

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}


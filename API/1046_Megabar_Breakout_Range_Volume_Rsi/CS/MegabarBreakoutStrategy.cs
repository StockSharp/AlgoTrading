using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Megabar Breakout strategy based on range, volume and RSI.
/// </summary>
public class MegabarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _rangeAvgPeriod;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiMaPeriod;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _filterTradeTime;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _rsiSma;
	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _rangeSma;

	/// <summary>
	/// Volume average period.
	/// </summary>
	public int VolumeAveragePeriod { get => _volumeAvgPeriod.Value; set => _volumeAvgPeriod.Value = value; }

	/// <summary>
	/// Volume breakout multiplier.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// Range average period.
	/// </summary>
	public int RangeAveragePeriod { get => _rangeAvgPeriod.Value; set => _rangeAvgPeriod.Value = value; }

	/// <summary>
	/// Range breakout multiplier.
	/// </summary>
	public decimal RangeMultiplier { get => _rangeMultiplier.Value; set => _rangeMultiplier.Value = value; }

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI moving average period.
	/// </summary>
	public int RsiMaPeriod { get => _rsiMaPeriod.Value; set => _rsiMaPeriod.Value = value; }

	/// <summary>
	/// Minimum RSI MA value for long trades.
	/// </summary>
	public decimal LongRsiThreshold { get => _longThreshold.Value; set => _longThreshold.Value = value; }

	/// <summary>
	/// Maximum RSI MA value for short trades.
	/// </summary>
	public decimal ShortRsiThreshold { get => _shortThreshold.Value; set => _shortThreshold.Value = value; }

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Trade only between 06:00 and 16:00.
	/// </summary>
	public bool FilterTradeHours { get => _filterTradeTime.Value; set => _filterTradeTime.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="MegabarBreakoutStrategy"/>.
	/// </summary>
	public MegabarBreakoutStrategy()
	{
		_volumeAvgPeriod = Param(nameof(VolumeAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Period for volume average", "Strategy Parameters");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier for volume breakout", "Strategy Parameters");

		_rangeAvgPeriod = Param(nameof(RangeAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range Average Period", "Period for range average", "Strategy Parameters");

		_rangeMultiplier = Param(nameof(RangeMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier for range breakout", "Strategy Parameters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Strategy Parameters");

		_rsiMaPeriod = Param(nameof(RsiMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI MA Period", "Period for RSI moving average", "Strategy Parameters");

		_longThreshold = Param(nameof(LongRsiThreshold), 50m)
			.SetDisplay("Long RSI Threshold", "Minimum RSI MA value for long trades", "Strategy Parameters");

		_shortThreshold = Param(nameof(ShortRsiThreshold), 70m)
			.SetDisplay("Short RSI Threshold", "Maximum RSI MA value for short trades", "Strategy Parameters");

		_takeProfit = Param(nameof(TakeProfit), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price points", "Strategy Parameters");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Strategy Parameters");

		_filterTradeTime = Param(nameof(FilterTradeHours), false)
			.SetDisplay("Filter Trade Hours", "Allow trades only between 06:00-16:00", "Strategy Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiSma = new SimpleMovingAverage { Length = RsiMaPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumeAveragePeriod };
		_rangeSma = new SimpleMovingAverage { Length = RangeAveragePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _rsiSma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _rsiSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal rsiMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAvg = _volumeSma.Process(candle.TotalVolume ?? 0m).ToDecimal();
		var range = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var rangeAvg = _rangeSma.Process(range).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_volumeSma.IsFormed || !_rangeSma.IsFormed || !_rsiSma.IsFormed)
			return;

		var volumeOk = (candle.TotalVolume ?? 0m) > volumeAvg * VolumeMultiplier;
		var rangeOk = range > rangeAvg * RangeMultiplier;
		var timeOk = !FilterTradeHours || IsWithinTradeHours(candle.OpenTime.TimeOfDay);

		if (candle.ClosePrice > candle.OpenPrice && volumeOk && rangeOk && rsiMaValue > LongRsiThreshold && timeOk && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.ClosePrice < candle.OpenPrice && volumeOk && rangeOk && rsiMaValue < ShortRsiThreshold && timeOk && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private static bool IsWithinTradeHours(TimeSpan time)
	{
		return time >= TimeSpan.FromHours(6) && time < TimeSpan.FromHours(16);
	}
}

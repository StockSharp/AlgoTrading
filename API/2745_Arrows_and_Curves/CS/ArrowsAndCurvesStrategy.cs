using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MT5 Arrows and Curves expert advisor using StockSharp high level API.
/// </summary>
public class ArrowsAndCurvesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _sspPeriod;
	private readonly StrategyParam<int> _channelPercent;
	private readonly StrategyParam<int> _channelStopPercent;
	private readonly StrategyParam<int> _relayShift;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highSeries = new();
	private readonly List<decimal> _lowSeries = new();
	private readonly List<decimal> _closeSeries = new();

	private bool _uptrend;
	private bool _uptrend2;
	private bool _previousSellArrow;
	private bool _previousBuyArrow;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public decimal VolumeValue { get => _volume.Value; set => _volume.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public int SspPeriod { get => _sspPeriod.Value; set => _sspPeriod.Value = value; }
	public int ChannelPercent { get => _channelPercent.Value; set => _channelPercent.Value = value; }
	public int ChannelStopPercent { get => _channelStopPercent.Value; set => _channelStopPercent.Value = value; }
	public int RelayShift { get => _relayShift.Value; set => _relayShift.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArrowsAndCurvesStrategy()
	{
		_volume = Param(nameof(VolumeValue), 1m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Risk %", "Risk percent for dynamic sizing when volume is zero", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step (pips)", "Minimum movement before trailing updates", "Risk");

		_sspPeriod = Param(nameof(SspPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("SSP", "Lookback period of the custom channel", "Indicator");

		_channelPercent = Param(nameof(ChannelPercent), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Channel %", "Outer channel percentage", "Indicator");

		_channelStopPercent = Param(nameof(ChannelStopPercent), 30)
		.SetGreaterOrEqualZero()
		.SetDisplay("Channel Stop %", "Inner channel percentage", "Indicator");

		_relayShift = Param(nameof(RelayShift), 10)
		.SetGreaterOrEqualZero()
		.SetDisplay("Relay", "Shift used by the indicator", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for processing", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		AddCandle(candle);

		var shouldOpenBuy = _previousSellArrow;
		var shouldOpenSell = _previousBuyArrow;

		if (Position == 0)
		{
			if (shouldOpenBuy)
			OpenLong(candle);
			else if (shouldOpenSell)
			OpenShort(candle);
		}
		else
		{
			if (Position > 0 && shouldOpenSell)
			{
				CloseAndReset();
			}
			else if (Position < 0 && shouldOpenBuy)
			{
				CloseAndReset();
			}

			UpdateTrailing(candle);
			CheckRiskExits(candle);
		}

		if (!TryComputeSignals(out var buySignal, out var sellSignal))
		{
			_previousBuyArrow = false;
			_previousSellArrow = false;
			return;
		}

		_previousBuyArrow = buySignal;
		_previousSellArrow = sellSignal;
	}

	private void OpenLong(ICandleMessage candle)
	{
		var volume = GetOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0 ? candle.ClosePrice - ConvertPips(StopLossPips) : null;
		_takePrice = TakeProfitPips > 0 ? candle.ClosePrice + ConvertPips(TakeProfitPips) : null;
	}

	private void OpenShort(ICandleMessage candle)
	{
		var volume = GetOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0 ? candle.ClosePrice + ConvertPips(StopLossPips) : null;
		_takePrice = TakeProfitPips > 0 ? candle.ClosePrice - ConvertPips(TakeProfitPips) : null;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _entryPrice == null)
		return;

		var distance = ConvertPips(TrailingStopPips);
		if (distance <= 0m)
		return;

		var step = ConvertPips(TrailingStepPips);

		if (Position > 0)
		{
			var gain = candle.ClosePrice - _entryPrice.Value;
			if (gain > distance + step)
			{
				var newStop = candle.ClosePrice - distance;
				if (!_stopPrice.HasValue || _stopPrice.Value < newStop - step)
				_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			var gain = _entryPrice.Value - candle.ClosePrice;
			if (gain > distance + step)
			{
				var newStop = candle.ClosePrice + distance;
				if (!_stopPrice.HasValue || _stopPrice.Value > newStop + step)
				_stopPrice = newStop;
			}
		}
	}

	private void CheckRiskExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var stopHit = _stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value;
			var takeHit = _takePrice.HasValue && candle.HighPrice >= _takePrice.Value;

			if (stopHit || takeHit)
			CloseAndReset();
		}
		else if (Position < 0)
		{
			var stopHit = _stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value;
			var takeHit = _takePrice.HasValue && candle.LowPrice <= _takePrice.Value;

			if (stopHit || takeHit)
			CloseAndReset();
		}
	}

	private void CloseAndReset()
	{
		ClosePosition();
		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void AddCandle(ICandleMessage candle)
	{
		_highSeries.Add(candle.HighPrice);
		_lowSeries.Add(candle.LowPrice);
		_closeSeries.Add(candle.ClosePrice);

		var maxCount = RelayShift + SspPeriod + 5;
		TrimSeries(_highSeries, maxCount);
		TrimSeries(_lowSeries, maxCount);
		TrimSeries(_closeSeries, maxCount);
	}

	private static void TrimSeries(List<decimal> series, int maxCount)
	{
		var excess = series.Count - maxCount;
		if (excess > 0)
		series.RemoveRange(0, excess);
	}

	private bool TryComputeSignals(out bool buySignal, out bool sellSignal)
	{
		buySignal = false;
		sellSignal = false;

		if (_closeSeries.Count <= 1)
		return false;

		var start = RelayShift + 1;
		var end = start + SspPeriod;

		if (end > _highSeries.Count || end > _lowSeries.Count)
		return false;

		var close = GetSeriesValue(_closeSeries, 1);

		decimal high = decimal.MinValue;
		decimal low = decimal.MaxValue;

		for (var i = start; i < end; i++)
		{
			var h = GetSeriesValue(_highSeries, i);
			var l = GetSeriesValue(_lowSeries, i);

			if (h > high)
			high = h;

			if (l < low)
			low = l;
		}

		var range = high - low;
		var smax = high - (low - high) * ChannelPercent / 100m;
		var smin = low + range * ChannelPercent / 100m;
		var innerPercent = ChannelPercent + ChannelStopPercent;
		var smax2 = high - range * innerPercent / 100m;
		var smin2 = low + range * innerPercent / 100m;

		var uptrend = _uptrend;
		var uptrend2 = _uptrend2;
		var old = uptrend;
		var old2 = uptrend2;

		if (close < smin && close < smax && uptrend2)
		uptrend = false;

		if (close > smax && close > smin && !uptrend2)
		uptrend = true;

		if ((close > smax2 || close > smin2) && !uptrend)
		uptrend2 = false;

		if ((close < smin2 || close < smax2) && uptrend)
		uptrend2 = true;

		if (close < smin && close < smax && !uptrend2)
		{
			sellSignal = true;
			uptrend2 = true;
		}

		if (close > smax && close > smin && uptrend2)
		{
			buySignal = true;
			uptrend2 = false;
		}

		if (uptrend != old && !uptrend)
		sellSignal = true;

		if (uptrend != old && uptrend)
		buySignal = true;

		_uptrend = uptrend;
		_uptrend2 = uptrend2;

		return true;
	}

	private static decimal GetSeriesValue(List<decimal> series, int index)
	{
		var targetIndex = series.Count - 1 - index;
		return targetIndex >= 0 ? series[targetIndex] : 0m;
	}

	private decimal GetOrderVolume(decimal price)
	{
		if (VolumeValue > 0m)
		return VolumeValue;

		var portfolio = Portfolio;
		var portfolioValue = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m || RiskPercent <= 0m)
		return 1m;

		var riskAmount = portfolioValue * RiskPercent / 100m;
		var stopOffset = StopLossPips > 0 ? ConvertPips(StopLossPips) : price * 0.01m;

		if (stopOffset <= 0m)
		return 1m;

		var volume = riskAmount / stopOffset;
		return volume > 0m ? volume : 1m;
	}

	private decimal ConvertPips(int pips)
	{
		if (pips <= 0)
		return 0m;

		var pipSize = GetPipSize();
		return pipSize <= 0m ? 0m : pipSize * pips;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var scale = GetDecimalScale(step);
		var factor = scale is 3 or 5 ? 10m : 1m;
		return step * factor;
	}

	private static int GetDecimalScale(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MegabarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private readonly List<decimal> _volumes = new();
	private readonly List<decimal> _ranges = new();
	private int _barsFromSignal;

	public int AvgPeriod { get => _avgPeriod.Value; set => _avgPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MegabarBreakoutStrategy()
	{
		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Rolling average period", "General");
		_multiplier = Param(nameof(Multiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Volume and range multiplier", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null;
		_volumes.Clear();
		_ranges.Clear();
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_volumes.Clear();
		_ranges.Clear();
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var vol = candle.TotalVolume;
		var range = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		_volumes.Add(vol);
		_ranges.Add(range);
		if (_volumes.Count > AvgPeriod) _volumes.RemoveAt(0);
		if (_ranges.Count > AvgPeriod) _ranges.RemoveAt(0);

		if (!_rsi.IsFormed || _volumes.Count < AvgPeriod)
			return;

		var avgVol = _volumes.Average();
		var avgRange = _ranges.Average();

		var volumeOk = vol > avgVol * Multiplier;
		var rangeOk = range > avgRange * Multiplier;
		_barsFromSignal++;

		if (_barsFromSignal >= SignalCooldownBars && candle.ClosePrice > candle.OpenPrice && volumeOk && rangeOk && rsiValue > 52m && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && candle.ClosePrice < candle.OpenPrice && volumeOk && rangeOk && rsiValue < 48m && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}

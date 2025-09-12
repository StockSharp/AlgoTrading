using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP mean reversion with RSI and volume filter.
/// </summary>
public class VwapMeanMagnetV2VolFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _vwapLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<bool> _volFilterEnabled;
	private readonly StrategyParam<int> _volLookback;
	private readonly StrategyParam<decimal> _volMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _volSma;

	public int VwapLength { get => _vwapLength.Value; set => _vwapLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public bool VolFilterEnabled { get => _volFilterEnabled.Value; set => _volFilterEnabled.Value = value; }
	public int VolLookback { get => _volLookback.Value; set => _volLookback.Value = value; }
	public decimal VolMultiplier { get => _volMultiplier.Value; set => _volMultiplier.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapMeanMagnetV2VolFilterStrategy()
	{
		_vwapLength = Param(nameof(VwapLength), 60).SetDisplay("VWAP Length").SetCanOptimize(true);
		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI Length").SetCanOptimize(true);
		_rsiOverbought = Param(nameof(RsiOverbought), 65).SetDisplay("RSI Overbought").SetCanOptimize(true);
		_rsiOversold = Param(nameof(RsiOversold), 25).SetDisplay("RSI Oversold").SetCanOptimize(true);
		_volFilterEnabled = Param(nameof(VolFilterEnabled), true).SetDisplay("Volume Filter");
		_volLookback = Param(nameof(VolLookback), 20).SetDisplay("Volume Lookback").SetCanOptimize(true);
		_volMultiplier = Param(nameof(VolMultiplier), 3m).SetDisplay("Volume Multiplier").SetCanOptimize(true);
		_stopLossPercent = Param(nameof(StopLossPercent), 0.5m).SetDisplay("Stop Loss %").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var vwap = new VolumeWeightedMovingAverage { Length = VwapLength };
		var rsi = new RSI { Length = RsiLength };
		_volSma = new SMA { Length = VolLookback };

		StartProtection(stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(vwap, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volAvg = _volSma.Process(candle.TotalVolume);
		var volOk = !VolFilterEnabled || candle.TotalVolume > volAvg * VolMultiplier;

		if (volOk)
		{
			if (candle.ClosePrice < vwapValue && rsiValue < RsiOversold && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice > vwapValue && rsiValue > RsiOverbought && Position >= 0)
				SellMarket();
		}

		if (Position > 0 && candle.ClosePrice >= vwapValue)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice <= vwapValue)
			BuyMarket();
	}
}


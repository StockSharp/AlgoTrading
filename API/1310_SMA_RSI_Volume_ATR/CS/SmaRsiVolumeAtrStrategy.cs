namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SmaRsiVolumeAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private SMA _volumeSma;
	private decimal _prevAtr;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TakeProfitPerc { get => _takeProfitPerc.Value; set => _takeProfitPerc.Value = value; }
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmaRsiVolumeAtrStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period for price SMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 30, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetRange(50, 90)
			.SetDisplay("RSI Overbought", "RSI overbought level", "General");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetRange(10, 50)
			.SetDisplay("RSI Oversold", "RSI oversold level", "General");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold", "Multiple of average volume", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "Period for ATR", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 30, 1);

		_takeProfitPerc = Param(nameof(TakeProfitPerc), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (%)", "Take profit percent", "Risk");

		_stopLossPerc = Param(nameof(StopLossPerc), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (%)", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAtr = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		_sma = new SMA { Length = SmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SMA { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(_sma, _rsi, _atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPerc, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPerc, UnitTypes.Percent));

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var avgVolume = _volumeSma.Process(candle.TotalVolume ?? 0m, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_sma.IsFormed || !_rsi.IsFormed || !_atr.IsFormed || !_volumeSma.IsFormed)
		{
			_prevAtr = atr;
			return;
		}

		var buyCondition = candle.ClosePrice > sma &&
			rsi < RsiOversold &&
			candle.TotalVolume > avgVolume * VolumeThreshold &&
			atr > _prevAtr;

		var sellCondition = candle.ClosePrice < sma &&
			rsi > RsiOverbought &&
			candle.TotalVolume > avgVolume * VolumeThreshold &&
			atr > _prevAtr;

		if (buyCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sellCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevAtr = atr;
	}
}


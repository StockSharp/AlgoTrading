using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vinicius Setup ATR Strategy.
/// Uses SuperTrend direction, RSI, ATR and volume filter to find strong momentum candles.
/// </summary>
public class ViniciusSetupATRStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _minBodyPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma = null!;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// SuperTrend multiplier.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Minimal body size in ATR fractions.
	/// </summary>
	public decimal MinBodyPercent { get => _minBodyPercent.Value; set => _minBodyPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ViniciusSetupATRStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetDisplay("ATR Length", "ATR period", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_factor = Param(nameof(Factor), 6m)
			.SetDisplay("Multiplier", "SuperTrend multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(2m, 10m, 1m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_minBodyPercent = Param(nameof(MinBodyPercent), 1m)
			.SetDisplay("Min Body %", "Minimal body size in ATR fractions", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend { Length = AtrLength, Multiplier = Factor };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stValue.IsFinal || !rsiValue.IsFinal || !atrValue.IsFinal)
			return;

		var st = (SuperTrendIndicatorValue)stValue;
		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		var volAvg = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		if (!_volumeSma.IsFormed)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isStrong = body > atr * MinBodyPercent;
		var isHighVol = candle.TotalVolume > volAvg * 1.2m;

		var buySignal = st.IsUpTrend && isStrong && isHighVol && rsi < 70m;
		var sellSignal = !st.IsUpTrend && isStrong && isHighVol && rsi > 30m;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

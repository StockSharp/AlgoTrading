using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vinicius Setup ATR Strategy.
/// Uses EMA trend direction, RSI, and ATR to find strong momentum candles.
/// </summary>
public class ViniciusSetupATRStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _minBodyPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rsiVal;
	private decimal _prevRsi;
	private int _cooldown;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal MinBodyPercent { get => _minBodyPercent.Value; set => _minBodyPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ViniciusSetupATRStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetDisplay("ATR Length", "ATR period", "General")
			.SetOptimize(5, 30, 1);

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter length", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "General")
			.SetOptimize(5, 30, 1);

		_minBodyPercent = Param(nameof(MinBodyPercent), 0.5m)
			.SetDisplay("Min Body %", "Minimal body size in ATR fractions", "General")
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_rsiVal = 0;
		_prevRsi = 0;

		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_rsiVal = 0;
		_prevRsi = 0;

		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, (candle, r) =>
			{
				_prevRsi = _rsiVal;
				_rsiVal = r;
			})
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (_rsiVal == 0 || _prevRsi == 0)
			return;

		var isUpTrend = candle.ClosePrice > emaVal;
		var isDownTrend = candle.ClosePrice < emaVal;

		var buySignal = isUpTrend && _prevRsi <= 50m && _rsiVal > 50m;
		var sellSignal = isDownTrend && _prevRsi >= 50m && _rsiVal < 50m;

		if (buySignal && Position <= 0)
		{
			BuyMarket();
			_cooldown = 100;
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket();
			_cooldown = 100;
		}
	}
}

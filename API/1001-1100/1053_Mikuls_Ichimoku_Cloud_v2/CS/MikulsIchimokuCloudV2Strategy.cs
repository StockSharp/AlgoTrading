using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mikul's Ichimoku Cloud v2 strategy.
/// Breakout strategy with ATR trailing stop.
/// </summary>
public class MikulsIchimokuCloudV2Strategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouBPeriod;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private AverageTrueRange _atr;
	private decimal? _trailPrice;
	private decimal? _prevTenkan;
	private decimal? _prevKijun;
	private int _barsFromSignal;
	private int _barIndex;
	private int _entryBarIndex;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }
	public int SenkouBPeriod { get => _senkouBPeriod.Value; set => _senkouBPeriod.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MikulsIchimokuCloudV2Strategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "General");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Trailing ATR multiplier", "General");
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Ichimoku Tenkan period", "General");
		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Ichimoku Kijun period", "General");
		_senkouBPeriod = Param(nameof(SenkouBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou B Period", "Ichimoku SenkouB period", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 50)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
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
		_ichimoku = null;
		_atr = null;
		_trailPrice = null;
		_prevTenkan = null;
		_prevKijun = null;
		_barsFromSignal = 0;
		_barIndex = 0;
		_entryBarIndex = -1;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trailPrice = null;
		_prevTenkan = null;
		_prevKijun = null;
		_barsFromSignal = SignalCooldownBars;
		_barIndex = 0;
		_entryBarIndex = -1;

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouBPeriod },
		};

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_ichimoku, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		_barsFromSignal++;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var atr = atrValue.ToDecimal();

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan)
			return;
		if (ichimokuTyped.Kijun is not decimal kijun)
			return;
		if (ichimokuTyped.SenkouA is not decimal senkouA)
			return;
		if (ichimokuTyped.SenkouB is not decimal senkouB)
			return;

		var upperCloud = Math.Max(senkouA, senkouB);
		var lowerCloud = Math.Min(senkouA, senkouB);

		var crossUp = _prevTenkan.HasValue && _prevKijun.HasValue
			&& _prevTenkan.Value <= _prevKijun.Value && tenkan > kijun;
		var crossDown = _prevTenkan.HasValue && _prevKijun.HasValue
			&& _prevTenkan.Value >= _prevKijun.Value && tenkan < kijun;

		var entrySignal = crossUp && tenkan > kijun;

		if (_barsFromSignal >= SignalCooldownBars && entrySignal && Position <= 0)
		{
			BuyMarket();
			_trailPrice = close - atr * AtrMultiplier;
			_entryBarIndex = _barIndex;
			_barsFromSignal = 0;
		}

		// Trailing stop using ATR + Ichimoku exit, skipped on entry bar.
		if (Position > 0 && _barIndex > _entryBarIndex)
		{
			var atrValue2 = atr * AtrMultiplier;
			var nextTrail = close - atrValue2;

			if (_trailPrice == null || nextTrail > _trailPrice)
				_trailPrice = nextTrail;

			if (close <= _trailPrice)
			{
				SellMarket();
				_trailPrice = null;
			}
			else if (crossDown || close < lowerCloud)
			{
				SellMarket();
				_trailPrice = null;
			}
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}
}

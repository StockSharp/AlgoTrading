using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Nova Futures PRO with HTF filter,
/// choppiness filter and squeeze breakout detection.
/// </summary>
public class NovaFuturesProSafeV6Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<decimal> _minAdx;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _kcLength;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<bool> _useHtf;
	private readonly StrategyParam<DataType> _htfCandleType;
	private readonly StrategyParam<int> _htfEmaLength;
	private readonly StrategyParam<decimal> _htfMinAdx;
	private readonly StrategyParam<bool> _useChop;
	private readonly StrategyParam<int> _chopLength;
	private readonly StrategyParam<decimal> _chopMax;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageDirectionalIndex _adx;
	private DirectionalIndex _di;
	private BollingerBands _bb;
	private KeltnerChannels _kc;
	private DonchianChannels _donchian;
	private ChoppinessIndex _chop;

	private ExponentialMovingAverage _htfEma;
	private AverageDirectionalIndex _htfAdx;

	private bool _squeezePrev;
	private decimal _prevDonHigh;
	private decimal _prevDonLow;
	private int _barsSinceExit;
	private decimal _prevPosition;
	private bool _htfOkLong;
	private bool _htfOkShort;

	/// <summary>
	/// Base EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// DMI length.
	/// </summary>
	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	/// <summary>
	/// Minimum ADX value to consider trend.
	/// </summary>
	public decimal MinAdx
	{
		get => _minAdx.Value;
		set => _minAdx.Value = value;
	}

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Keltner period.
	/// </summary>
	public int KcLength
	{
		get => _kcLength.Value;
		set => _kcLength.Value = value;
	}

	/// <summary>
	/// Keltner multiplier.
	/// </summary>
	public decimal KcMultiplier
	{
		get => _kcMultiplier.Value;
		set => _kcMultiplier.Value = value;
	}

	/// <summary>
	/// Donchian length.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Use higher timeframe filter.
	/// </summary>
	public bool UseHtf
	{
		get => _useHtf.Value;
		set => _useHtf.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HtfCandleType
	{
		get => _htfCandleType.Value;
		set => _htfCandleType.Value = value;
	}

	/// <summary>
	/// HTF EMA length.
	/// </summary>
	public int HtfEmaLength
	{
		get => _htfEmaLength.Value;
		set => _htfEmaLength.Value = value;
	}

	/// <summary>
	/// Minimum HTF ADX.
	/// </summary>
	public decimal HtfMinAdx
	{
		get => _htfMinAdx.Value;
		set => _htfMinAdx.Value = value;
	}

	/// <summary>
	/// Use choppiness filter.
	/// </summary>
	public bool UseChop
	{
		get => _useChop.Value;
		set => _useChop.Value = value;
	}

	/// <summary>
	/// Choppiness period.
	/// </summary>
	public int ChopLength
	{
		get => _chopLength.Value;
		set => _chopLength.Value = value;
	}

	/// <summary>
	/// Maximum choppiness allowed.
	/// </summary>
	public decimal ChopMax
	{
		get => _chopMax.Value;
		set => _chopMax.Value = value;
	}

	/// <summary>
	/// Cooldown bars after exit.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Main candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NovaFuturesProSafeV6Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 200)
			.SetDisplay("EMA Length", "Base EMA length", "Trend");
		_dmiLength = Param(nameof(DmiLength), 14)
			.SetDisplay("DMI Length", "Directional Movement length", "Trend");
		_minAdx = Param(nameof(MinAdx), 18m)
			.SetDisplay("Min ADX", "Minimum ADX for trending market", "Trend");
		_bbLength = Param(nameof(BbLength), 20)
			.SetDisplay("BB Length", "Bollinger Bands period", "Volatility");
		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetDisplay("BB Mult", "Bollinger Bands multiplier", "Volatility");
		_kcLength = Param(nameof(KcLength), 20)
			.SetDisplay("KC Length", "Keltner Channels period", "Volatility");
		_kcMultiplier = Param(nameof(KcMultiplier), 1.6m)
			.SetDisplay("KC Mult", "Keltner Channels multiplier", "Volatility");
		_donchianLength = Param(nameof(DonchianLength), 20)
			.SetDisplay("Donchian Length", "Lookback for structure", "Structure");
		_useHtf = Param(nameof(UseHtf), true)
			.SetDisplay("Use HTF", "Enable higher timeframe filter", "HTF");
		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("HTF Candle", "Higher timeframe", "HTF");
		_htfEmaLength = Param(nameof(HtfEmaLength), 200)
			.SetDisplay("HTF EMA", "EMA length for higher timeframe", "HTF");
		_htfMinAdx = Param(nameof(HtfMinAdx), 18m)
			.SetDisplay("HTF Min ADX", "Minimum ADX on higher timeframe", "HTF");
		_useChop = Param(nameof(UseChop), true)
			.SetDisplay("Use Choppiness", "Enable choppiness filter", "Choppiness");
		_chopLength = Param(nameof(ChopLength), 14)
			.SetDisplay("Chop Length", "Choppiness period", "Choppiness");
		_chopMax = Param(nameof(ChopMax), 61.8m)
			.SetDisplay("Chop Threshold", "Maximum choppiness allowed", "Choppiness");
		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown", "Bars to wait after exit", "Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main timeframe", "General");
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
		_squeezePrev = false;
		_prevDonHigh = 0m;
		_prevDonLow = 0m;
		_barsSinceExit = CooldownBars;
		_prevPosition = 0m;
		_htfOkLong = true;
		_htfOkShort = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_adx = new AverageDirectionalIndex { Length = DmiLength };
		_di = new DirectionalIndex { Length = DmiLength };
		_bb = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_kc = new KeltnerChannels { Length = KcLength, Multiplier = KcMultiplier };
		_donchian = new DonchianChannels { Length = DonchianLength };
		_chop = new ChoppinessIndex { Length = ChopLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ema, _adx, _di, _bb, _kc, _donchian, _chop, ProcessCandle)
			.Start();

		if (UseHtf)
		{
			_htfEma = new ExponentialMovingAverage { Length = HtfEmaLength };
			_htfAdx = new AverageDirectionalIndex { Length = DmiLength };

			var htfSub = SubscribeCandles(HtfCandleType);
			htfSub
				.BindEx(_htfEma, _htfAdx, ProcessHtf)
				.Start();
		}
	}

	private void ProcessHtf(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var ema = emaValue.ToDecimal();
		_htfOkLong = candle.ClosePrice > ema && adx >= HtfMinAdx;
		_htfOkShort = candle.ClosePrice < ema && adx >= HtfMinAdx;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue adxValue, IIndicatorValue diValue, IIndicatorValue bbValue, IIndicatorValue kcValue, IIndicatorValue donchValue, IIndicatorValue chopValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var dx = adxTyped.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		var ema = emaValue.ToDecimal();

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal bbU || bb.LowBand is not decimal bbL || bb.MovingAverage is not decimal bbBasis)
			return;

		var kc = (KeltnerChannelsValue)kcValue;
		if (kc.Upper is not decimal kcU || kc.Lower is not decimal kcL)
			return;

		var don = (DonchianChannelsValue)donchValue;
		if (don.UpBand is not decimal donHigh || don.LowBand is not decimal donLow)
			return;

		var chop = chopValue.ToDecimal();

		var squeezeOn = (bbU - bbL) < (kcU - kcL);
		var releaseNow = _squeezePrev && !squeezeOn;
		_squeezePrev = squeezeOn;

		var bosUp = candle.ClosePrice > _prevDonHigh;
		var bosDown = candle.ClosePrice < _prevDonLow;
		_prevDonHigh = donHigh;
		_prevDonLow = donLow;

		var trending = adx >= MinAdx;
		var trendLong = trending && candle.ClosePrice > ema && plusDi > minusDi;
		var trendShort = trending && candle.ClosePrice < ema && minusDi > plusDi;
		var volLong = releaseNow && candle.ClosePrice > bbBasis;
		var volShort = releaseNow && candle.ClosePrice < bbBasis;
		var engLong = (trendLong ? 1 : 0) + (bosUp ? 1 : 0) + (volLong ? 1 : 0);
		var engShort = (trendShort ? 1 : 0) + (bosDown ? 1 : 0) + (volShort ? 1 : 0);
		var rawLong = engLong >= 2;
		var rawShort = engShort >= 2;
		var chopOk = !UseChop || chop <= ChopMax;
		var longSig = rawLong && chopOk && (!UseHtf || _htfOkLong);
		var shortSig = rawShort && chopOk && (!UseHtf || _htfOkShort);
		var canTrade = _barsSinceExit >= CooldownBars;

		if (canTrade && longSig && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (canTrade && shortSig && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (_prevPosition != 0m && Position == 0m)
			_barsSinceExit = 0;
		else if (Position == 0m)
			_barsSinceExit++;
		else
			_barsSinceExit = 0;

		_prevPosition = Position;
	}
}

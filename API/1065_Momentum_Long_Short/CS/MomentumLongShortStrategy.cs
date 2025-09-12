using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy trading long and short with optional filters.
/// </summary>
public class MomentumLongShortStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<decimal> _slPercentLong;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<string> _smoothType;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _rsiLengthLong;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _atrLength;

	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<decimal> _slPercentShort;
	private readonly StrategyParam<decimal> _tpPercentShort;
	private readonly StrategyParam<int> _rsiLengthShort;
	private readonly StrategyParam<decimal> _rsiThresholdShort;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<bool> _useAtrFilterShort;
	private readonly StrategyParam<bool> _useStrongUptrendBlock;
	private readonly StrategyParam<decimal> _shortTrendGapPct;

	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _rsiSmooth;
	private IIndicator _adxSmooth;
	private IIndicator _atrSmooth;
	private IIndicator _atrShortSmooth;

	public MomentumLongShortStrategy()
	{
		_enableLongs = Param(nameof(EnableLongs), true)
			.SetDisplay("Enable Long Trades", "Allow long entries", "Long");
		_slPercentLong = Param(nameof(SlPercentLong), 3m)
			.SetDisplay("Long Stop Loss %", "Long stop loss percent", "Long");
		_useRsiFilter = Param(nameof(UseRsiFilter), false)
			.SetDisplay("Enable RSI Filter", "Use RSI filter", "Long Filters");
		_useAdxFilter = Param(nameof(UseAdxFilter), false)
			.SetDisplay("Enable ADX Filter", "Use ADX filter", "Long Filters");
		_useAtrFilter = Param(nameof(UseAtrFilter), false)
			.SetDisplay("Enable ATR Filter", "Use ATR filter", "Long Filters");
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
			.SetDisplay("Require MA100 > MA500", "Trend alignment filter", "Long Filters");
		_smoothType = Param(nameof(SmoothType), "EMA")
			.SetDisplay("Smoothing Type", "EMA or SMA", "Filters");
		_smoothingLength = Param(nameof(SmoothingLength), 100)
			.SetDisplay("Smoothing Length", "Length for smoothing", "Filters");
		_rsiLengthLong = Param(nameof(RsiLengthLong), 14)
			.SetDisplay("RSI Length", "RSI length", "RSI");
		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "ADX length", "ADX");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR length", "ATR");

		_enableShorts = Param(nameof(EnableShorts), false)
			.SetDisplay("Enable Short Trades", "Allow short entries", "Short");
		_slPercentShort = Param(nameof(SlPercentShort), 3m)
			.SetDisplay("Short Stop Loss %", "Short stop loss percent", "Short");
		_tpPercentShort = Param(nameof(TpPercentShort), 4m)
			.SetDisplay("Short Take Profit %", "Short take profit percent", "Short");
		_rsiLengthShort = Param(nameof(RsiLengthShort), 14)
			.SetDisplay("RSI Length", "RSI length", "Short Filters");
		_rsiThresholdShort = Param(nameof(RsiThresholdShort), 33m)
			.SetDisplay("RSI Threshold", "RSI threshold", "Short Filters");
		_bbLength = Param(nameof(BbLength), 20)
			.SetDisplay("Bollinger Length", "Bollinger band length", "Short Filters");
		_useAtrFilterShort = Param(nameof(UseAtrFilterShort), true)
			.SetDisplay("Enable ATR Filter", "Use ATR filter", "Short Filters");
		_useStrongUptrendBlock = Param(nameof(UseStrongUptrendBlock), true)
			.SetDisplay("Block Shorts in Strong Uptrend", "Block shorts if MA100 significantly above MA500", "Short Filters");
		_shortTrendGapPct = Param(nameof(ShortTrendGapPct), 2m)
			.SetDisplay("Threshold %", "Threshold for blocking shorts", "Short Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	public bool EnableLongs { get => _enableLongs.Value; set => _enableLongs.Value = value; }
	public decimal SlPercentLong { get => _slPercentLong.Value; set => _slPercentLong.Value = value; }
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }
	public bool UseAtrFilter { get => _useAtrFilter.Value; set => _useAtrFilter.Value = value; }
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	public string SmoothType { get => _smoothType.Value; set => _smoothType.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public int RsiLengthLong { get => _rsiLengthLong.Value; set => _rsiLengthLong.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }
	public decimal SlPercentShort { get => _slPercentShort.Value; set => _slPercentShort.Value = value; }
	public decimal TpPercentShort { get => _tpPercentShort.Value; set => _tpPercentShort.Value = value; }
	public int RsiLengthShort { get => _rsiLengthShort.Value; set => _rsiLengthShort.Value = value; }
	public decimal RsiThresholdShort { get => _rsiThresholdShort.Value; set => _rsiThresholdShort.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public bool UseAtrFilterShort { get => _useAtrFilterShort.Value; set => _useAtrFilterShort.Value = value; }
	public bool UseStrongUptrendBlock { get => _useStrongUptrendBlock.Value; set => _useStrongUptrendBlock.Value = value; }
	public decimal ShortTrendGapPct { get => _shortTrendGapPct.Value; set => _shortTrendGapPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiSmooth?.Reset();
		_adxSmooth?.Reset();
		_atrSmooth?.Reset();
		_atrShortSmooth?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var ma100 = CreateMa(100);
		var ma500 = CreateMa(500);
		var rsiLong = new RelativeStrengthIndex { Length = RsiLengthLong };
		_rsiSmooth = CreateMa(SmoothingLength);
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		_adxSmooth = CreateMa(SmoothingLength);
		var atr = new AverageTrueRange { Length = AtrLength };
		_atrSmooth = CreateMa(SmoothingLength);
		_atrShortSmooth = CreateMa(SmoothingLength);
		var rsiShort = new RelativeStrengthIndex { Length = RsiLengthShort };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma100, ma500, rsiLong, adx, atr, rsiShort, bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma100);
			DrawIndicator(area, ma500);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma100, decimal ma500, decimal rsiLong, decimal adx, decimal atr, decimal rsiShort, decimal bbMiddle, decimal bbUpper, decimal bbLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsiSmoothVal = _rsiSmooth!.Process(rsiLong);
		var adxSmoothVal = _adxSmooth!.Process(adx);
		var atrSmoothVal = _atrSmooth!.Process(atr);
		var atrShortSmoothVal = _atrShortSmooth!.Process(atr);

		if (!_rsiSmooth.IsFormed || !_adxSmooth.IsFormed || !_atrSmooth.IsFormed || !_atrShortSmooth.IsFormed)
			return;

		var priceAboveMAs = candle.ClosePrice > ma100 && candle.ClosePrice > ma500;
		var trendAlignment = !UseTrendFilter || ma100 > ma500;
		var rsiPass = !UseRsiFilter || rsiLong > rsiSmoothVal.GetValue<decimal>();
		var adxPass = !UseAdxFilter || adx > adxSmoothVal.GetValue<decimal>();
		var atrPass = !UseAtrFilter || atr > atrSmoothVal.GetValue<decimal>();

		var priceBelowMAs = candle.ClosePrice < ma100 && candle.ClosePrice < ma500;
		var priceBelowBB = candle.ClosePrice < bbLower;
		var rsiOversold = rsiShort < RsiThresholdShort;
		var atrShortPass = !UseAtrFilterShort || atr > atrShortSmoothVal.GetValue<decimal>();
		var emaGapTooWide = (ma100 - ma500) / ma500 * 100m > ShortTrendGapPct;
		var strongUptrendBlock = !UseStrongUptrendBlock || !emaGapTooWide;

		var longCondition = EnableLongs && priceAboveMAs && trendAlignment && rsiPass && adxPass && atrPass;
		var shortCondition = EnableShorts && priceBelowMAs && priceBelowBB && rsiOversold && atrShortPass && strongUptrendBlock;

		if (longCondition && Position <= 0)
			BuyMarket();
		if (shortCondition && Position >= 0)
			SellMarket();

		var longStop = PositionAvgPrice * (1 - SlPercentLong / 100m);
		if (Position > 0)
		{
			if (candle.LowPrice <= longStop || candle.ClosePrice < ma500)
				SellMarket(Position);
		}

		var shortStop = PositionAvgPrice * (1 + SlPercentShort / 100m);
		var shortTp = PositionAvgPrice * (1 - TpPercentShort / 100m);
		if (Position < 0)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTp)
				BuyMarket(-Position);
		}
	}

	private IIndicator CreateMa(int length)
		=> SmoothType == "EMA" ? new EMA { Length = length } : new SMA { Length = length };
}

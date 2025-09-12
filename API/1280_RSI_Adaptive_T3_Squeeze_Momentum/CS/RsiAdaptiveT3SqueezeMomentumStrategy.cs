using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-Adaptive T3 combined with Squeeze Momentum.
/// The strategy detects squeeze release and uses an RSI based
/// adaptive Tillson T3 for trend direction.
/// </summary>
public class RsiAdaptiveT3SqueezeMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minT3Length;
	private readonly StrategyParam<int> _maxT3Length;
	private readonly StrategyParam<decimal> _t3VolumeFactor;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<bool> _useTrueRange;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _linReg;
	private decimal? _e1, _e2, _e3, _e4, _e5, _e6;
	private decimal? _prevT3, _prevPrevT3;

	/// <summary>
	/// RSI period for adaptive T3.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Minimum T3 length.
	/// </summary>
	public int MinT3Length
	{
		get => _minT3Length.Value;
		set => _minT3Length.Value = value;
	}

	/// <summary>
	/// Maximum T3 length.
	/// </summary>
	public int MaxT3Length
	{
		get => _maxT3Length.Value;
		set => _maxT3Length.Value = value;
	}

	/// <summary>
	/// T3 volume factor.
	/// </summary>
	public decimal T3VolumeFactor
	{
		get => _t3VolumeFactor.Value;
		set => _t3VolumeFactor.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Keltner Channels period.
	/// </summary>
	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	/// <summary>
	/// Keltner Channels multiplier.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Use true range for Keltner Channels.
	/// </summary>
	public bool UseTrueRange
	{
		get => _useTrueRange.Value;
		set => _useTrueRange.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RsiAdaptiveT3SqueezeMomentumStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period for adaptive T3", "T3")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_minT3Length = Param(nameof(MinT3Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Min T3 Length", "Minimum Tillson T3 length", "T3");

		_maxT3Length = Param(nameof(MaxT3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Max T3 Length", "Maximum Tillson T3 length", "T3");

		_t3VolumeFactor = Param(nameof(T3VolumeFactor), 0.7m)
			.SetGreaterThanZero()
			.SetDisplay("T3 Volume Factor", "Tillson T3 volume factor", "T3");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 27)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Squeeze");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands width", "Squeeze");

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("KC Length", "Keltner Channels period", "Squeeze");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("KC Multiplier", "Keltner Channels multiplier", "Squeeze");

		_useTrueRange = Param(nameof(UseTrueRange), true)
			.SetDisplay("Use True Range", "Use true range for Keltner Channels", "Squeeze");

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
		_e1 = _e2 = _e3 = _e4 = _e5 = _e6 = _prevT3 = _prevPrevT3 = null;
		_linReg = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_linReg = new LinearRegression { Length = KeltnerPeriod };

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerMultiplier };
		var keltner = new KeltnerChannels { Length = KeltnerPeriod, Multiplier = KeltnerMultiplier, UseTrueRange = UseTrueRange };
		var donchian = new DonchianChannels { Length = KeltnerPeriod };
		var sma = new SMA { Length = KeltnerPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, keltner, donchian, sma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, keltner);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue keltnerValue, IIndicatorValue donchianValue, IIndicatorValue smaValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!bollingerValue.IsFinal || !keltnerValue.IsFinal || !donchianValue.IsFinal || !smaValue.IsFinal || !rsiValue.IsFinal)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		var kc = (KeltnerChannelsValue)keltnerValue;
		var dc = (DonchianChannelsValue)donchianValue;

		if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower || kc.Upper is not decimal kcUpper || kc.Lower is not decimal kcLower || dc.Middle is not decimal mid)
			return;

		var avgClose = smaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();

		var diff = candle.ClosePrice - (mid + avgClose) / 2m;
		var lrValue = (LinearRegressionValue)_linReg.Process(diff, candle.OpenTime, true);
		if (lrValue.LinearReg is not decimal val)
			return;

		var rsiScale = 1m - rsi / 100m;
		var len = Math.Round(MinT3Length + (MaxT3Length - MinT3Length) * rsiScale);

		var e1 = UpdateEma(ref _e1, candle.ClosePrice, len);
		var e2 = UpdateEma(ref _e2, e1, len);
		var e3 = UpdateEma(ref _e3, e2, len);
		var e4 = UpdateEma(ref _e4, e3, len);
		var e5 = UpdateEma(ref _e5, e4, len);
		var e6 = UpdateEma(ref _e6, e5, len);

		var v = T3VolumeFactor;
		var c1 = -v * v * v;
		var c2 = 3m * v * v + 3m * v * v * v;
		var c3 = -6m * v * v - 3m * v - 3m * v * v * v;
		var c4 = 1m + 3m * v + v * v * v + 3m * v * v;
		var t3 = c1 * e6 + c2 * e5 + c3 * e4 + c4 * e3;

		var sqzOff = bbLower < kcLower && bbUpper > kcUpper;

		var longCondition = false;
		var shortCondition = false;

		if (_prevT3.HasValue && _prevPrevT3.HasValue)
		{
			longCondition = _prevT3 <= _prevPrevT3 && t3 > _prevT3 && val > 0 && sqzOff;
			shortCondition = _prevT3 >= _prevPrevT3 && t3 < _prevT3 && val < 0 && sqzOff;
		}

		_prevPrevT3 = _prevT3;
		_prevT3 = t3;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private static decimal UpdateEma(ref decimal? prev, decimal input, decimal length)
	{
		var alpha = 2m / (length + 1m);
		var value = prev is null ? input : alpha * input + (1m - alpha) * prev.Value;
		prev = value;
		return value;
	}
}

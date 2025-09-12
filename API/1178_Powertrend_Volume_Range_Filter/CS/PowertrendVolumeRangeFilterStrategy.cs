using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume range filter strategy with optional ADX, high/low and VWMA filters.
/// Enters long when price breaks the upper band and exits when price breaks the lower band.
/// </summary>
public class PowertrendVolumeRangeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<int> _lengthAdx;
	private readonly StrategyParam<bool> _useHl;
	private readonly StrategyParam<int> _lengthHl;
	private readonly StrategyParam<bool> _useVwma;
	private readonly StrategyParam<int> _lengthVwma;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _smoothRng = null!;
	private AverageDirectionalIndex _adx = null!;
	private SimpleMovingAverage _adxSma = null!;
	private SimpleMovingAverage _vwma = null!;
	private Highest _highBandTrend = null!;
	private Lowest _lowBandTrend = null!;

	private decimal? _prevVolRng;
	private decimal? _prevVolume;
	private decimal? _prevBase;
	private decimal? _prevClose;
	private decimal? _prevHighBand;
	private decimal? _prevLowBand;
	private decimal? _prevHighBandTrend;
	private decimal? _prevLowBandTrend;
	private int _barsSinceCrossOver = int.MaxValue;
	private int _barsSinceCrossUnder = int.MaxValue;

	/// <summary>
	/// Initializes a new instance of <see cref="PowertrendVolumeRangeFilterStrategy"/>.
	/// </summary>
	public PowertrendVolumeRangeFilterStrategy()
	{
		_length = Param(nameof(Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Main length for range calculations", "General");

		_useAdx = Param(nameof(UseAdx), false)
			.SetDisplay("Use ADX", "Enable ADX filter", "Filters");

		_lengthAdx = Param(nameof(LengthAdx), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Period for ADX filter", "Filters");

		_useHl = Param(nameof(UseHl), false)
			.SetDisplay("Use Range HL", "Enable high/low trend filter", "Filters");

		_lengthHl = Param(nameof(LengthHl), 14)
			.SetGreaterThanZero()
			.SetDisplay("HL Length", "Lookback for trend filter", "Filters");

		_useVwma = Param(nameof(UseVwma), false)
			.SetDisplay("Use VWMA", "Enable VWMA filter", "Filters");

		_lengthVwma = Param(nameof(LengthVwma), 200)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Length", "Period for VWMA filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// Main length for range calculations.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Enable ADX filter.
	/// </summary>
	public bool UseAdx { get => _useAdx.Value; set => _useAdx.Value = value; }

	/// <summary>
	/// ADX period length.
	/// </summary>
	public int LengthAdx { get => _lengthAdx.Value; set => _lengthAdx.Value = value; }

	/// <summary>
	/// Enable high/low trend filter.
	/// </summary>
	public bool UseHl { get => _useHl.Value; set => _useHl.Value = value; }

	/// <summary>
	/// Lookback for trend filter.
	/// </summary>
	public int LengthHl { get => _lengthHl.Value; set => _lengthHl.Value = value; }

	/// <summary>
	/// Enable VWMA filter.
	/// </summary>
	public bool UseVwma { get => _useVwma.Value; set => _useVwma.Value = value; }

	/// <summary>
	/// VWMA period.
	/// </summary>
	public int LengthVwma { get => _lengthVwma.Value; set => _lengthVwma.Value = value; }

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smoothRng = new AverageTrueRange { Length = Length };
		_adx = new AverageDirectionalIndex { Length = LengthAdx };
		_adxSma = new SimpleMovingAverage { Length = LengthAdx };
		_vwma = new SimpleMovingAverage { Length = LengthVwma };
		_highBandTrend = new Highest { Length = LengthHl };
		_lowBandTrend = new Lowest { Length = LengthHl };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_adx, _smoothRng, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal adxValue, decimal smoothRng)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume ?? 0m;

		var volRng = ComputeVolRng(close, volume, smoothRng);

		var hband = volRng + smoothRng;
		var lowband = volRng - smoothRng;

		var uprng = _prevBase is decimal pb && volRng > pb;

		var adxSmaValue = _adxSma.Process(adxValue).ToNullableDecimal();
		if (UseAdx && adxSmaValue is null)
		{
			UpdateState(close, volume, volRng, hband, lowband, null, null);
			return;
		}
		var adxFilter = !UseAdx || adxValue > adxSmaValue;

		var highBandTrendFollow = _highBandTrend.Process(hband).ToNullableDecimal();
		var lowBandTrendFollow = _lowBandTrend.Process(lowband).ToNullableDecimal();
		if (UseHl && (highBandTrendFollow is null || lowBandTrendFollow is null))
		{
			UpdateState(close, volume, volRng, hband, lowband, highBandTrendFollow, lowBandTrendFollow);
			return;
		}

		bool crossOver = false;
		bool crossUnder = false;

		if (_prevClose is decimal pc)
		{
			if (_prevHighBandTrend is decimal ph && highBandTrendFollow is decimal hb && pc <= ph && close > ph)
				crossOver = true;
			if (_prevLowBandTrend is decimal pl && lowBandTrendFollow is decimal lb && pc >= pl && close < pl)
				crossUnder = true;
		}

		if (crossOver)
			_barsSinceCrossOver = 0;
		else if (_barsSinceCrossOver < int.MaxValue)
			_barsSinceCrossOver++;

		if (crossUnder)
			_barsSinceCrossUnder = 0;
		else if (_barsSinceCrossUnder < int.MaxValue)
			_barsSinceCrossUnder++;

		var inGeneralUptrend = _barsSinceCrossOver < _barsSinceCrossUnder;
		var iguFilterPositive = !UseHl || inGeneralUptrend;
		var iguFilterNegative = !UseHl || !inGeneralUptrend;

		var vwmaValue = _vwma.Process(volRng).ToNullableDecimal();
		if (UseVwma && vwmaValue is null)
		{
			UpdateState(close, volume, volRng, hband, lowband, highBandTrendFollow, lowBandTrendFollow);
			return;
		}
		var vwmaFilterPositive = !UseVwma || volRng > vwmaValue;

		bool crossOverHband = _prevClose is decimal pc2 && _prevHighBand is decimal phb && pc2 <= phb && close > hband;
		bool crossUnderLowband = _prevClose is decimal pc3 && _prevLowBand is decimal plb && pc3 >= plb && close < lowband;

		var buy = uprng && crossOverHband && iguFilterPositive && adxFilter && vwmaFilterPositive;
		var sell = !uprng && crossUnderLowband && iguFilterNegative && adxFilter;

		if (buy && Position <= 0)
			BuyMarket();
		else if (sell && Position > 0)
			SellMarket();

		UpdateState(close, volume, volRng, hband, lowband, highBandTrendFollow, lowBandTrendFollow);
	}

	private void UpdateState(decimal close, decimal volume, decimal volRng, decimal hband, decimal lowband, decimal? highBandTrendFollow, decimal? lowBandTrendFollow)
	{
		_prevVolume = volume;
		_prevVolRng = volRng;
		_prevBase = volRng;
		_prevClose = close;
		_prevHighBand = hband;
		_prevLowBand = lowband;
		_prevHighBandTrend = highBandTrendFollow;
		_prevLowBandTrend = lowBandTrendFollow;
	}

	private decimal ComputeVolRng(decimal source, decimal volume, decimal smoothRng)
	{
		if (_prevVolRng is null)
			return source;

		var prev = _prevVolRng.Value;
		var prevVol = _prevVolume ?? 0m;

		if (volume > prevVol)
			return Math.Max(prev, source - smoothRng);
		else
			return Math.Min(prev, source + smoothRng);
	}
}

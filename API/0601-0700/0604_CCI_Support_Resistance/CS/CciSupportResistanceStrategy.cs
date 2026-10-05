using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Support Resistance strategy.
/// A CCI(CciLength) pivot low confirmed by LeftPivot lower values before it and RightPivot after it sets support at the low of its
/// candle; a CCI pivot high sets resistance at the high of its candle. A candle whose low touches support (within Buffer price steps)
/// and closes above it goes long, one whose high touches resistance and closes below it goes short, reversing an opposite position.
/// With TrendMatter the trend must agree: in Cross mode EMA(FastMaLength) against EMA(SlowMaLength), in Slope mode the change of the
/// slow EMA over SlopeLength candles. Each entry freezes a stop Ksl ATRs and a target Ktp ATRs away.
/// </summary>
public class CciSupportResistanceStrategy : Strategy
{
	/// <summary>
	/// Trend filter type.
	/// </summary>
	public enum TrendTypes
	{
		/// <summary>
		/// Fast EMA against slow EMA.
		/// </summary>
		Cross,

		/// <summary>
		/// Slope of the slow EMA.
		/// </summary>
		Slope,
	}

	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _leftPivot;
	private readonly StrategyParam<int> _rightPivot;
	private readonly StrategyParam<decimal> _buffer;
	private readonly StrategyParam<bool> _trendMatter;
	private readonly StrategyParam<TrendTypes> _trendType;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<decimal> _ksl;
	private readonly StrategyParam<decimal> _ktp;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal cci, decimal high, decimal low)> _history = [];
	private readonly List<decimal> _slowHistory = [];
	private decimal? _support;
	private decimal? _resistance;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	/// <summary>
	/// CCI values before a pivot.
	/// </summary>
	public int LeftPivot
	{
		get => _leftPivot.Value;
		set => _leftPivot.Value = value;
	}

	/// <summary>
	/// CCI values after a pivot.
	/// </summary>
	public int RightPivot
	{
		get => _rightPivot.Value;
		set => _rightPivot.Value = value;
	}

	/// <summary>
	/// Touch tolerance in price steps.
	/// </summary>
	public decimal Buffer
	{
		get => _buffer.Value;
		set => _buffer.Value = value;
	}

	/// <summary>
	/// Require the trend filter.
	/// </summary>
	public bool TrendMatter
	{
		get => _trendMatter.Value;
		set => _trendMatter.Value = value;
	}

	/// <summary>
	/// Trend filter type.
	/// </summary>
	public TrendTypes TrendType
	{
		get => _trendType.Value;
		set => _trendType.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Candles the slow EMA slope spans.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// ATR multiple of the stop.
	/// </summary>
	public decimal Ksl
	{
		get => _ksl.Value;
		set => _ksl.Value = value;
	}

	/// <summary>
	/// ATR multiple of the target.
	/// </summary>
	public decimal Ktp
	{
		get => _ktp.Value;
		set => _ktp.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public CciSupportResistanceStrategy()
	{
		_cciLength = Param(nameof(CciLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "CCI");

		_leftPivot = Param(nameof(LeftPivot), 50)
			.SetGreaterThanZero()
			.SetDisplay("Left Pivot", "CCI values before a pivot", "CCI");

		_rightPivot = Param(nameof(RightPivot), 50)
			.SetGreaterThanZero()
			.SetDisplay("Right Pivot", "CCI values after a pivot", "CCI");

		_buffer = Param(nameof(Buffer), 10m)
			.SetNotNegative()
			.SetDisplay("Buffer", "Touch tolerance in price steps", "Levels");

		_trendMatter = Param(nameof(TrendMatter), true)
			.SetDisplay("Trend Matter", "Require the trend filter", "Trend");

		_trendType = Param(nameof(TrendType), TrendTypes.Cross)
			.SetDisplay("Trend Type", "Trend filter type", "Trend");

		_slowMaLength = Param(nameof(SlowMaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Slow EMA period", "Trend");

		_fastMaLength = Param(nameof(FastMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Fast EMA period", "Trend");

		_slopeLength = Param(nameof(SlopeLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Candles the slow EMA slope spans", "Trend");

		_ksl = Param(nameof(Ksl), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Ksl", "ATR multiple of the stop", "Risk");

		_ktp = Param(nameof(Ktp), 2.2m)
			.SetGreaterThanZero()
			.SetDisplay("Ktp", "ATR multiple of the target", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_history.Clear();
		_slowHistory.Clear();
		_support = null;
		_resistance = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var cci = new CommodityChannelIndex { Length = CciLength };
		var fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, fastMa, slowMa, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void UpdateLevels(decimal cci, ICandleMessage candle)
	{
		_history.Add((cci, candle.HighPrice, candle.LowPrice));

		var size = LeftPivot + RightPivot + 1;

		if (_history.Count > size)
			_history.RemoveAt(0);

		if (_history.Count < size)
			return;

		var pivot = _history[LeftPivot];
		var isHigh = true;
		var isLow = true;

		for (var i = 0; i < size && (isHigh || isLow); i++)
		{
			if (i == LeftPivot)
				continue;

			var value = _history[i].cci;

			if (value >= pivot.cci)
				isHigh = false;

			if (value <= pivot.cci)
				isLow = false;
		}

		if (isHigh)
			_resistance = pivot.high;

		if (isLow)
			_support = pivot.low;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (cciValue.IsFormed)
			UpdateLevels(cciValue.GetValue<decimal>(), candle);

		if (!slowValue.IsFormed)
			return;

		var slow = slowValue.GetValue<decimal>();
		_slowHistory.Add(slow);

		if (_slowHistory.Count > SlopeLength + 1)
			_slowHistory.RemoveAt(0);

		if (!fastValue.IsFormed || !atrValue.IsFormed || _slowHistory.Count <= SlopeLength)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Stop and target frozen at entry.
		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget && (candle.LowPrice <= longStop || candle.HighPrice >= longTarget))
		{
			SellMarket(Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget && (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		bool bullish;
		bool bearish;

		if (!TrendMatter)
		{
			bullish = true;
			bearish = true;
		}
		else if (TrendType == TrendTypes.Cross)
		{
			var fast = fastValue.GetValue<decimal>();
			bullish = fast > slow;
			bearish = fast < slow;
		}
		else
		{
			var slope = slow - _slowHistory.First();
			bullish = slope > 0;
			bearish = slope < 0;
		}

		var tolerance = Buffer * (Security?.PriceStep ?? 1m);
		var close = candle.ClosePrice;
		var atr = atrValue.GetValue<decimal>();

		if (bullish && _support is decimal support && candle.LowPrice <= support + tolerance && close > support && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - atr * Ksl;
			_targetPrice = close + atr * Ktp;
		}
		else if (bearish && _resistance is decimal resistance && candle.HighPrice >= resistance - tolerance && close < resistance && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + atr * Ksl;
			_targetPrice = close - atr * Ktp;
		}
	}
}

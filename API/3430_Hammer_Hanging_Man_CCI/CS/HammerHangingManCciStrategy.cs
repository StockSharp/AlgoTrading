using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades hammer and hanging man candlestick patterns with CCI confirmation.
/// </summary>
public class HammerHangingManCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _longConfirmationThreshold;
	private readonly StrategyParam<decimal> _shortConfirmationThreshold;
	private readonly StrategyParam<decimal> _exitUpperThreshold;
	private readonly StrategyParam<decimal> _exitLowerThreshold;

	private CommodityChannelIndex _cci = null!;
	private SimpleMovingAverage _closeSma = null!;

	private ICandleMessage? _prevCandle;
	private ICandleMessage? _prevPrevCandle;

	private decimal? _prevCci;
	private decimal? _prevPrevCci;

	private decimal? _prevSma;
	private decimal? _prevPrevSma;

	/// <summary>
	/// Initializes a new instance of the <see cref="HammerHangingManCciStrategy"/> class.
	/// </summary>
	public HammerHangingManCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for pattern detection", "General");

		_cciPeriod = Param(nameof(CciPeriod), 11)
		.SetRange(5, 50)
		.SetDisplay("CCI Period", "Number of bars for the CCI indicator", "Indicators")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 5)
		.SetRange(3, 30)
		.SetDisplay("MA Period", "Number of bars for the trend filter SMA", "Indicators")
		.SetCanOptimize(true);

		_longConfirmationThreshold = Param(nameof(LongConfirmationThreshold), 40m)
		.SetRange(-100m, 100m)
		.SetDisplay("Long CCI Threshold", "Maximum CCI value to allow hammer confirmation", "Signals")
		.SetCanOptimize(true);

		_shortConfirmationThreshold = Param(nameof(ShortConfirmationThreshold), 60m)
		.SetRange(-100m, 100m)
		.SetDisplay("Short CCI Threshold", "Minimum CCI value to allow hanging man confirmation", "Signals")
		.SetCanOptimize(true);

		_exitUpperThreshold = Param(nameof(ExitUpperThreshold), 70m)
		.SetRange(-100m, 200m)
		.SetDisplay("Upper Exit Threshold", "CCI level that forces an exit after crossing downward or upward", "Risk")
		.SetCanOptimize(true);

		_exitLowerThreshold = Param(nameof(ExitLowerThreshold), 30m)
		.SetRange(-100m, 100m)
		.SetDisplay("Lower Exit Threshold", "Secondary CCI level for aggressive exits", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars for the CCI calculation.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars in the moving average trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Maximum CCI value accepted for hammer confirmation.
	/// </summary>
	public decimal LongConfirmationThreshold
	{
		get => _longConfirmationThreshold.Value;
		set => _longConfirmationThreshold.Value = value;
	}

	/// <summary>
	/// Minimum CCI value accepted for hanging man confirmation.
	/// </summary>
	public decimal ShortConfirmationThreshold
	{
		get => _shortConfirmationThreshold.Value;
		set => _shortConfirmationThreshold.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold that triggers exits after upward crossings.
	/// </summary>
	public decimal ExitUpperThreshold
	{
		get => _exitUpperThreshold.Value;
		set => _exitUpperThreshold.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold that triggers exits after upward or downward crossings.
	/// </summary>
	public decimal ExitLowerThreshold
	{
		get => _exitLowerThreshold.Value;
		set => _exitLowerThreshold.Value = value;
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

		_prevCandle = null;
		_prevPrevCandle = null;
		_prevCci = null;
		_prevPrevCci = null;
		_prevSma = null;
		_prevPrevSma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeSma = new SimpleMovingAverage { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_closeSma, _cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeSma);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal cciValue)
	{
		// The strategy only reacts to completed candles.
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Skip trading decisions until both indicators are fully formed.
		if (!_closeSma.IsFormed || !_cci.IsFormed)
		{
			ShiftCaches(candle, smaValue, cciValue);
			return;
		}

		var prevCandle = _prevCandle;
		var prevPrevCandle = _prevPrevCandle;
		var prevCci = _prevCci;
		var prevPrevCci = _prevPrevCci;
		var prevPrevSma = _prevPrevSma;

		if (prevCandle != null && prevPrevCandle != null && prevCci.HasValue && prevPrevCci.HasValue && prevPrevSma.HasValue)
		{
			if (IsFormedAndOnlineAndAllowTrading())
			{
				var hammerDetected = IsHammer(prevCandle, prevPrevCandle, prevPrevSma.Value);
				var hangingManDetected = IsHangingMan(prevCandle, prevPrevCandle, prevPrevSma.Value);

				if (hammerDetected && prevCci.Value < LongConfirmationThreshold && Position <= 0)
				{
					// Enter long on hammer with oversold CCI confirmation.
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else if (hangingManDetected && prevCci.Value > ShortConfirmationThreshold && Position >= 0)
				{
					// Enter short on hanging man with overbought CCI confirmation.
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
			}

			if (Position > 0 && ShouldExitLong(prevCci.Value, prevPrevCci.Value))
			{
				// Close the long position when CCI crosses exit thresholds from below.
				SellMarket(Position);
			}
			else if (Position < 0 && ShouldExitShort(prevCci.Value, prevPrevCci.Value))
			{
				// Close the short position when CCI crosses exit thresholds from above.
				BuyMarket(Math.Abs(Position));
			}
		}

		ShiftCaches(candle, smaValue, cciValue);
	}

	private void ShiftCaches(ICandleMessage candle, decimal smaValue, decimal cciValue)
	{
		// Preserve the previous candle references and indicator readings for future checks.
		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;

		_prevPrevSma = _prevSma;
		_prevSma = smaValue;
	}

	private static bool IsHammer(ICandleMessage candle, ICandleMessage reference, decimal trendValue)
	{
		var bodyTop = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyBottom = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var candleRange = candle.HighPrice - candle.LowPrice;

		if (candleRange == 0)
		{
			return false;
		}

		var upperThirdLevel = candle.HighPrice - candleRange / 3m;
		var downTrend = (candle.HighPrice + candle.LowPrice) / 2m < trendValue;
		var bodyInUpperThird = bodyBottom > upperThirdLevel;
		var gapDown = candle.ClosePrice < reference.ClosePrice && candle.OpenPrice < reference.OpenPrice;

		return downTrend && bodyInUpperThird && gapDown;
	}

	private static bool IsHangingMan(ICandleMessage candle, ICandleMessage reference, decimal trendValue)
	{
		var bodyTop = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyBottom = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var candleRange = candle.HighPrice - candle.LowPrice;

		if (candleRange == 0)
		{
			return false;
		}

		var upperThirdLevel = candle.HighPrice - candleRange / 3m;
		var upTrend = (candle.HighPrice + candle.LowPrice) / 2m > trendValue;
		var bodyInUpperThird = bodyBottom > upperThirdLevel;
		var gapUp = candle.ClosePrice > reference.ClosePrice && candle.OpenPrice > reference.OpenPrice;

		return upTrend && bodyInUpperThird && gapUp;
	}

	private bool ShouldExitLong(decimal latestCci, decimal previousCci)
	{
		// Exit long when CCI crosses above the configured thresholds.
		var crossUpper = previousCci < ExitUpperThreshold && latestCci > ExitUpperThreshold;
		var crossLower = previousCci < ExitLowerThreshold && latestCci > ExitLowerThreshold;
		return crossUpper || crossLower;
	}

	private bool ShouldExitShort(decimal latestCci, decimal previousCci)
	{
		// Exit short when CCI crosses below the configured thresholds.
		var crossUpper = previousCci > ExitUpperThreshold && latestCci < ExitUpperThreshold;
		var crossLower = previousCci > ExitLowerThreshold && latestCci < ExitLowerThreshold;
		return crossUpper || crossLower;
	}
}

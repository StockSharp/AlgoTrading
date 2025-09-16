using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OsMA histogram pattern strategy converted from the "OsMaSter v0" MQL expert.
/// Enters on four-bar momentum reversals identified by the MACD histogram.
/// </summary>
public class OsMaSterV0Strategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _histCurrent;
	private decimal? _histPrev1;
	private decimal? _histPrev2;
	private decimal? _histPrev3;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OsMaSterV0Strategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 9)
			.SetDisplay("Fast EMA", "Fast EMA period for MACD histogram", "Indicators")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period for MACD histogram", "Indicators")
			.SetGreaterThanZero();

		_signalPeriod = Param(nameof(SignalPeriod), 5)
			.SetDisplay("Signal Smoothing", "Signal moving average period", "Indicators")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume in lots", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");

		Volume = TradeVolume;
	}

	/// <summary>
	/// Fast EMA period used in MACD histogram calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used in MACD histogram calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal moving average period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss size expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit size expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_histCurrent = null;
		_histPrev1 = null;
		_histPrev2 = null;
		_histPrev3 = null;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		var macd = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var pipSize = (decimals == 3 || decimals == 5) ? step * 10m : step;

		// Convert pip-based risk controls into absolute price offsets.
		StartProtection(
			takeProfit: TakeProfitPips > 0 ? new Unit(TakeProfitPips * pipSize, UnitTypes.Point) : null,
			stopLoss: StopLossPips > 0 ? new Unit(StopLossPips * pipSize, UnitTypes.Point) : null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Ignore incomplete candles because signals rely on closed data.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is synchronized with the market and permitted to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceHistogramValue)macdValue;
		if (macdTyped.Macd is not decimal histogram)
			return;

		// Shift stored histogram values to maintain the last four observations.
		_histPrev3 = _histPrev2;
		_histPrev2 = _histPrev1;
		_histPrev1 = _histCurrent;
		_histCurrent = histogram;

		if (_histCurrent is not decimal hist0 ||
			_histPrev1 is not decimal hist1 ||
			_histPrev2 is not decimal hist2 ||
			_histPrev3 is not decimal hist3)
		{
			return;
		}

		// Detect the specific four-bar rising pattern used for long entries.
		var bullishPattern = hist3 > hist2 && hist2 < hist1 && hist1 < hist0;

		// Detect the mirrored falling pattern used for short entries.
		var bearishPattern = hist3 < hist2 && hist2 > hist1 && hist1 > hist0;

		// The original expert opens a position only when no trades are active.
		if (Position != 0)
			return;

		if (bullishPattern)
		{
			// Enter long when the histogram forms a higher-low sequence.
			BuyMarket();
		}
		else if (bearishPattern)
		{
			// Enter short when the histogram forms a lower-high sequence.
			SellMarket();
		}
	}
}

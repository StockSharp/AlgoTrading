using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy converted from the OsMaSter_V0 MetaTrader 4 expert.
/// Uses the MACD histogram (OsMA) turning points to detect reversals.
/// </summary>
public class OsMaMasterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _appliedPrice;
	private readonly StrategyParam<int> _shift1;
	private readonly StrategyParam<int> _shift2;
	private readonly StrategyParam<int> _shift3;
	private readonly StrategyParam<int> _shift4;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private MovingAverageConvergenceDivergence? _macd;
	private decimal?[] _osmaHistory = Array.Empty<decimal?>();
	private int _historyCount;

	private decimal? _pipSize;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTarget;
	private decimal? _shortTarget;

	/// <summary>
	/// Primary candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period from the MACD oscillator.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period from the MACD oscillator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period from the MACD oscillator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mapping that matches the MetaTrader PRICE_* constants.
	/// </summary>
	public int AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// First OsMA shift parameter (usually 0).
	/// </summary>
	public int Shift1
	{
		get => _shift1.Value;
		set => _shift1.Value = value;
	}

	/// <summary>
	/// Second OsMA shift parameter (usually 1).
	/// </summary>
	public int Shift2
	{
		get => _shift2.Value;
		set => _shift2.Value = value;
	}

	/// <summary>
	/// Third OsMA shift parameter (usually 2).
	/// </summary>
	public int Shift3
	{
		get => _shift3.Value;
		set => _shift3.Value = value;
	}

	/// <summary>
	/// Fourth OsMA shift parameter (usually 3).
	/// </summary>
	public int Shift4
	{
		get => _shift4.Value;
		set => _shift4.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Set to zero to disable the protective stop.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Set to zero to disable the target.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Creates the strategy with parameters mirroring the MQL expert defaults.
	/// </summary>
	public OsMaMasterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for MACD calculations", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA length used inside MACD", "Indicators")
			.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Slow EMA length used inside MACD", "Indicators")
			.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal EMA length used inside MACD", "Indicators")
			.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), 0)
			.SetDisplay("Applied Price", "MetaTrader PRICE_* code controlling the MACD input", "General")
			.SetCanOptimize(true);

		_shift1 = Param(nameof(Shift1), 0)
			.SetDisplay("Shift 1", "First OsMA shift parameter", "Logic");

		_shift2 = Param(nameof(Shift2), 1)
			.SetDisplay("Shift 2", "Second OsMA shift parameter", "Logic");

		_shift3 = Param(nameof(Shift3), 2)
			.SetDisplay("Shift 3", "Third OsMA shift parameter", "Logic");

		_shift4 = Param(nameof(Shift4), 3)
			.SetDisplay("Shift 4", "Fourth OsMA shift parameter", "Logic");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Take Profit (pips)", "Profit target distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		Volume = 0.01m;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_osmaHistory = Array.Empty<decimal?>();
		_historyCount = 0;
		_pipSize = null;
		ResetTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		if (_pipSize == null || _pipSize <= 0m)
		{
			var step = Security?.PriceStep;
			_pipSize = step > 0m ? step : 1m;
		}

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = FastEmaPeriod,
			LongLength = SlowEmaPeriod,
			SignalLength = SignalPeriod
		};

		var maxShift = Math.Max(Math.Max(Math.Max(Math.Max(GetShiftIndex(Shift1), GetShiftIndex(Shift2)), GetShiftIndex(Shift3)), GetShiftIndex(Shift4)), 0);
		_osmaHistory = new decimal?[maxShift + 1];
		_historyCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_macd == null || _pipSize == null)
			return;

		ManagePosition(candle);

		var price = GetAppliedPrice(candle, AppliedPrice);
		var macdValue = _macd.Process(new DecimalIndicatorValue(_macd, price, candle.OpenTime));

		if (!macdValue.IsFinal || !_macd.IsFormed || macdValue is not MovingAverageConvergenceDivergenceValue macdData)
			return;

		UpdateHistory(macdData.Histogram);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryGetOsma(GetShiftIndex(Shift1), out var os1) ||
			!TryGetOsma(GetShiftIndex(Shift2), out var os2) ||
			!TryGetOsma(GetShiftIndex(Shift3), out var os3) ||
			!TryGetOsma(GetShiftIndex(Shift4), out var os4))
		{
			return;
		}

		var bullishReversal = os4 > os3 && os3 < os2 && os2 < os1;
		var bearishReversal = os4 < os3 && os3 > os2 && os2 > os1;

		if (Position != 0)
			return;

		if (bullishReversal)
		{
			BuyMarket();
			SetLongTargets(candle.ClosePrice);
		}
		else if (bearishReversal)
		{
			SellMarket();
			SetShortTargets(candle.ClosePrice);
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Math.Abs(Position);

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value && volume > 0m)
			{
				SellMarket(volume);
				ResetTargets();
				return;
			}

			if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value && volume > 0m)
			{
				SellMarket(volume);
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value && volume > 0m)
			{
				BuyMarket(volume);
				ResetTargets();
				return;
			}

			if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value && volume > 0m)
			{
				BuyMarket(volume);
				ResetTargets();
			}
		}
	}

	private void SetLongTargets(decimal entryPrice)
	{
		var pip = _pipSize ?? 0m;
		_longStop = StopLossPips > 0m && pip > 0m ? entryPrice - StopLossPips * pip : null;
		_longTarget = TakeProfitPips > 0m && pip > 0m ? entryPrice + TakeProfitPips * pip : null;
		_shortStop = null;
		_shortTarget = null;
	}

	private void SetShortTargets(decimal entryPrice)
	{
		var pip = _pipSize ?? 0m;
		_shortStop = StopLossPips > 0m && pip > 0m ? entryPrice + StopLossPips * pip : null;
		_shortTarget = TakeProfitPips > 0m && pip > 0m ? entryPrice - TakeProfitPips * pip : null;
		_longStop = null;
		_longTarget = null;
	}

	private void ResetTargets()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	private void UpdateHistory(decimal osma)
	{
		if (_osmaHistory.Length == 0)
			return;

		for (var i = _osmaHistory.Length - 1; i > 0; i--)
			_osmaHistory[i] = _osmaHistory[i - 1];

		_osmaHistory[0] = osma;

		if (_historyCount < _osmaHistory.Length)
			_historyCount++;
	}

	private bool TryGetOsma(int shift, out decimal value)
	{
		value = 0m;

		if (shift < 0 || shift >= _historyCount)
			return false;

		var stored = _osmaHistory[shift];
		if (stored is not decimal osma)
			return false;

		value = osma;
		return true;
	}

	private static int GetShiftIndex(int shift)
	{
		return Math.Max(0, shift);
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, int appliedPrice)
	{
		return appliedPrice switch
		{
			1 => candle.OpenPrice,
			2 => candle.HighPrice,
			3 => candle.LowPrice,
			4 => (candle.HighPrice + candle.LowPrice) / 2m,
			5 => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			6 => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}

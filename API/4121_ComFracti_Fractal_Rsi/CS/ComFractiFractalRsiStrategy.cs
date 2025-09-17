using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert ComFracti.
/// Detects fractal bias on two timeframes and confirms entries with a daily RSI filter.
/// Includes optional time-based exits and configurable stop-loss/take-profit distances.
/// </summary>
public class ComFractiFractalRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _expiryMinutes;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<int> _primaryBuyShift;
	private readonly StrategyParam<int> _higherBuyShift;
	private readonly StrategyParam<int> _primarySellShift;
	private readonly StrategyParam<int> _higherSellShift;
	private readonly StrategyParam<decimal> _rsiBuyOffset;
	private readonly StrategyParam<decimal> _rsiSellOffset;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _mainCandleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private readonly FractalSeries _mainFractals = new();
	private readonly FractalSeries _higherFractals = new();

	private RelativeStrengthIndex _dailyRsi = null!;
	private decimal? _latestDailyRsi;
	private decimal _pipSize;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private DateTimeOffset? _entryTime;
	private decimal _lastClosePrice;

	/// <summary>
	/// Initializes the strategy with the original ComFracti defaults.
	/// </summary>
	public ComFractiFractalRsiStrategy()
	{
		_volumeParam = Param(nameof(Volume), 0.1m)
		.SetDisplay("Volume", "Order size used for entries", "Trading")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 700m)
		.SetDisplay("Take Profit (pips)", "Distance to the profit target expressed in pips", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 2500m)
		.SetDisplay("Stop Loss (pips)", "Distance to the stop-loss expressed in pips", "Risk")
		.SetCanOptimize(true);

		_expiryMinutes = Param(nameof(ExpiryMinutes), 5555)
		.SetDisplay("Expiry (minutes)", "Close the position after this many minutes", "Risk")
		.SetCanOptimize(false);

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), false)
		.SetDisplay("Close On Opposite", "Exit when the signal reverses", "Trading")
		.SetCanOptimize(false);

		_primaryBuyShift = Param(nameof(PrimaryBuyShift), 3)
		.SetDisplay("Primary Buy Shift", "Bars back to inspect the fractal on the trading timeframe", "Signals")
		.SetCanOptimize(true);

		_higherBuyShift = Param(nameof(HigherBuyShift), 3)
		.SetDisplay("Higher Buy Shift", "Bars back to inspect the fractal on the higher timeframe", "Signals")
		.SetCanOptimize(true);

		_primarySellShift = Param(nameof(PrimarySellShift), 3)
		.SetDisplay("Primary Sell Shift", "Bars back to inspect the fractal on the trading timeframe for shorts", "Signals")
		.SetCanOptimize(true);

		_higherSellShift = Param(nameof(HigherSellShift), 3)
		.SetDisplay("Higher Sell Shift", "Bars back to inspect the higher timeframe fractal for shorts", "Signals")
		.SetCanOptimize(true);

		_rsiBuyOffset = Param(nameof(RsiBuyOffset), 3m)
		.SetDisplay("RSI Buy Offset", "Offset below 50 required to enable long setups", "Filters")
		.SetCanOptimize(true);

		_rsiSellOffset = Param(nameof(RsiSellOffset), 3m)
		.SetDisplay("RSI Sell Offset", "Offset above 50 required to enable short setups", "Filters")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
		.SetDisplay("RSI Period", "Length of the daily RSI filter", "Filters")
		.SetCanOptimize(true);

		_mainCandleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
		.SetDisplay("Main Timeframe", "Candle type used for trade execution", "Data")
		.SetCanOptimize(false);

		_higherCandleType = Param(nameof(HigherTimeFrame), DataType.TimeFrame(TimeSpan.FromHours(1)))
		.SetDisplay("Higher Timeframe", "Candle type used for trend confirmation", "Data")
		.SetCanOptimize(false);

		_dailyCandleType = Param(nameof(DailyTimeFrame), DataType.TimeFrame(TimeSpan.FromDays(1)))
		.SetDisplay("Daily Timeframe", "Candle type used for the RSI filter", "Data")
		.SetCanOptimize(false);
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Distance to the profit target expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance to the stop-loss expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Holding time after which the trade is closed automatically.
	/// </summary>
	public int ExpiryMinutes
	{
		get => _expiryMinutes.Value;
		set => _expiryMinutes.Value = value;
	}

	/// <summary>
	/// Exit trades when the signal flips to the opposite direction.
	/// </summary>
	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
	}

	/// <summary>
	/// Bars back to inspect the fractal on the trading timeframe for long setups.
	/// </summary>
	public int PrimaryBuyShift
	{
		get => _primaryBuyShift.Value;
		set => _primaryBuyShift.Value = value;
	}

	/// <summary>
	/// Bars back to inspect the fractal on the higher timeframe for long setups.
	/// </summary>
	public int HigherBuyShift
	{
		get => _higherBuyShift.Value;
		set => _higherBuyShift.Value = value;
	}

	/// <summary>
	/// Bars back to inspect the fractal on the trading timeframe for short setups.
	/// </summary>
	public int PrimarySellShift
	{
		get => _primarySellShift.Value;
		set => _primarySellShift.Value = value;
	}

	/// <summary>
	/// Bars back to inspect the higher timeframe fractal for short setups.
	/// </summary>
	public int HigherSellShift
	{
		get => _higherSellShift.Value;
		set => _higherSellShift.Value = value;
	}

	/// <summary>
	/// Offset applied below the RSI midpoint to allow long trades.
	/// </summary>
	public decimal RsiBuyOffset
	{
		get => _rsiBuyOffset.Value;
		set => _rsiBuyOffset.Value = value;
	}

	/// <summary>
	/// Offset applied above the RSI midpoint to allow short trades.
	/// </summary>
	public decimal RsiSellOffset
	{
		get => _rsiSellOffset.Value;
		set => _rsiSellOffset.Value = value;
	}

	/// <summary>
	/// Length of the daily RSI filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for trade execution.
	/// </summary>
	public DataType CandleType
	{
		get => _mainCandleType.Value;
		set => _mainCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the trend confirmation fractals.
	/// </summary>
	public DataType HigherTimeFrame
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Daily timeframe used to compute the RSI filter.
	/// </summary>
	public DataType DailyTimeFrame
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_mainFractals.Reset();
		_higherFractals.Reset();

		_dailyRsi = null!;
		_latestDailyRsi = null;
		_pipSize = 0m;
		_stopPrice = null;
		_takePrice = null;
		_entryTime = null;
		_lastClosePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_dailyRsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod,
		};

		_pipSize = GetPipSize();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(ProcessMainCandle)
		.Start();

		var higherSubscription = SubscribeCandles(HigherTimeFrame);
		higherSubscription
		.Bind(ProcessHigherCandle)
		.Start();

		var dailySubscription = SubscribeCandles(DailyTimeFrame);
		dailySubscription
		.Bind(ProcessDailyCandle)
		.Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_dailyRsi.Length = RsiPeriod;

		var indicatorValue = _dailyRsi.Process(candle.OpenPrice, candle.CloseTime, true);
		if (!indicatorValue.IsFinal)
		return;

		_latestDailyRsi = indicatorValue.ToDecimal();
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		_higherFractals.Update(candle);
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_mainFractals.Update(candle);
		_lastClosePrice = candle.ClosePrice;

		if (HandleStopsAndTargets(candle))
		return;

		if (HandleExpiry(candle))
		return;

		var signal = GetSignal();

		if (CloseOnOppositeSignal)
		{
			if (Position > 0m && signal < 0)
			{
				SellMarket(Position);
				return;
			}

			if (Position < 0m && signal > 0)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (signal > 0 && Position <= 0m)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
			return;
		}

		if (signal < 0 && Position >= 0m)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	private bool HandleStopsAndTargets(ICandleMessage candle)
	{
		if (Position == 0m)
		return false;

		if (_stopPrice is null && _takePrice is null)
		return false;

		if (Position > 0m)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				return true;
			}
		}

		return false;
	}

	private bool HandleExpiry(ICandleMessage candle)
	{
		if (Position == 0m)
		return false;

		if (ExpiryMinutes <= 0)
		return false;

		if (_entryTime is not DateTimeOffset entry)
		return false;

		if (candle.CloseTime - entry < TimeSpan.FromMinutes(ExpiryMinutes))
		return false;

		if (Position > 0m)
		SellMarket(Position);
		else
		BuyMarket(-Position);

		return true;
	}

	private int GetSignal()
	{
		if (_latestDailyRsi is not decimal rsi)
		return 0;

		var primaryBuy = _mainFractals.GetTrendSignal(PrimaryBuyShift);
		var higherBuy = _higherFractals.GetTrendSignal(HigherBuyShift);
		if (primaryBuy > 0 && higherBuy > 0 && rsi < 50m - RsiBuyOffset)
		return 1;

		var primarySell = _mainFractals.GetTrendSignal(PrimarySellShift);
		var higherSell = _higherFractals.GetTrendSignal(HigherSellShift);
		if (primarySell < 0 && higherSell < 0 && rsi > 50m + RsiSellOffset)
		return -1;

		return 0;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_stopPrice = null;
			_takePrice = null;
			_entryTime = null;
			return;
		}

		var entryPrice = PositionPrice ?? _lastClosePrice;
		_entryTime = CurrentTime;

		var takeDistance = TakeProfitPips * _pipSize;
		var stopDistance = StopLossPips * _pipSize;

		if (Position > 0m)
		{
			_takePrice = takeDistance > 0m ? entryPrice + takeDistance : (decimal?)null;
			_stopPrice = stopDistance > 0m ? entryPrice - stopDistance : (decimal?)null;
		}
		else
		{
			_takePrice = takeDistance > 0m ? entryPrice - takeDistance : (decimal?)null;
			_stopPrice = stopDistance > 0m ? entryPrice + stopDistance : (decimal?)null;
		}
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals ?? 0;

		if (decimals >= 3)
		return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private sealed class FractalSeries
	{
		private readonly List<CandleSnapshot> _candles = new();
		private readonly List<FractalDirection> _signals = new();

		public void Reset()
		{
			_candles.Clear();
			_signals.Clear();
		}

		public void Update(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			return;

			var data = new CandleSnapshot(candle.HighPrice, candle.LowPrice);
			_candles.Add(data);
			_signals.Add(FractalDirection.None);

			if (_candles.Count < 5)
			return;

			var centerIndex = _candles.Count - 3;

			var center = _candles[centerIndex];
			var prev2 = _candles[centerIndex - 2];
			var prev1 = _candles[centerIndex - 1];
			var next1 = _candles[centerIndex + 1];
			var next2 = _candles[centerIndex + 2];

			var isUpper = center.High > prev1.High && center.High > prev2.High && center.High > next1.High && center.High > next2.High;
			var isLower = center.Low < prev1.Low && center.Low < prev2.Low && center.Low < next1.Low && center.Low < next2.Low;

			if (isLower && !isUpper)
			{
				_signals[centerIndex] = FractalDirection.Lower;
			}
			else if (isUpper && !isLower)
			{
				_signals[centerIndex] = FractalDirection.Upper;
			}
			else if (!isUpper && !isLower)
			{
				_signals[centerIndex] = FractalDirection.None;
			}
		}

		public int GetTrendSignal(int shift)
		{
			if (shift < 0)
			return 0;

			var direction = GetDirection(shift);
			return direction switch
			{
				FractalDirection.Lower => 1,
				FractalDirection.Upper => -1,
				_ => 0,
			};
		}

		private FractalDirection GetDirection(int shift)
		{
			var index = _signals.Count - 1 - shift;
			if (index < 0 || index >= _signals.Count)
			return FractalDirection.None;

			return _signals[index];
		}

		private readonly struct CandleSnapshot
		{
			public CandleSnapshot(decimal high, decimal low)
			{
				High = high;
				Low = low;
			}

		public decimal High { get; }
		public decimal Low { get; }
		}

		private enum FractalDirection
		{
			None,
			Lower,
			Upper,
		}
	}
}

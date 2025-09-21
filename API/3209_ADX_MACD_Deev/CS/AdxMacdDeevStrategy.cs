using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the "ADX MACD Deev" MetaTrader Expert Advisor.
/// Combines ADX strength filtering with MACD momentum alignment to time entries.
/// Applies optional partial profit taking and trailing stop handling.
/// </summary>
public class AdxMacdDeevStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _takeHalfProfit;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _adxBarsInterval;
	private readonly StrategyParam<int> _adxMinimum;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _macdBarsInterval;
	private readonly StrategyParam<int> _macdMinimumPips;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal? _macd;
	private AverageDirectionalIndex? _adx;

	private decimal[] _macdMainHistory = Array.Empty<decimal>();
	private decimal[] _macdSignalHistory = Array.Empty<decimal>();
	private decimal[] _adxHistory = Array.Empty<decimal>();
	private int _macdHistoryCount;
	private int _adxHistoryCount;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _halfTaken;

	/// <summary>
	/// Trading volume for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price move required before trailing stop adjustment in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables partial profit taking when the take profit level is reached.
	/// </summary>
	public bool TakeHalfProfit
	{
		get => _takeHalfProfit.Value;
		set => _takeHalfProfit.Value = value;
	}

	/// <summary>
	/// ADX indicator averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Number of historical ADX bars required to be monotonically rising or falling.
	/// </summary>
	public int AdxBarsInterval
	{
		get => _adxBarsInterval.Value;
		set => _adxBarsInterval.Value = value;
	}

	/// <summary>
	/// Minimum ADX strength required for both entry directions.
	/// </summary>
	public int AdxMinimum
	{
		get => _adxMinimum.Value;
		set => _adxMinimum.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD component.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD component.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD smoothing.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Number of historical MACD bars that must align in the same direction.
	/// </summary>
	public int MacdBarsInterval
	{
		get => _macdBarsInterval.Value;
		set => _macdBarsInterval.Value = value;
	}

	/// <summary>
	/// Minimum MACD magnitude expressed in pips.
	/// </summary>
	public int MacdMinimumPips
	{
		get => _macdMinimumPips.Value;
		set => _macdMinimumPips.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="AdxMacdDeevStrategy"/> with default parameters.
	/// </summary>
	public AdxMacdDeevStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for new market entries", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 120, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
			.SetDisplay("Take Profit (pips)", "Initial take profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Additional distance before trailing", "Risk");

		_takeHalfProfit = Param(nameof(TakeHalfProfit), false)
			.SetDisplay("Take Half Profit", "Close half position on take profit", "Risk");

		_adxPeriod = Param(nameof(AdxPeriod), 6)
			.SetDisplay("ADX Period", "ADX averaging length", "Indicators")
			.SetGreaterThanZero();

		_adxBarsInterval = Param(nameof(AdxBarsInterval), 2)
			.SetDisplay("ADX Bars Interval", "Consecutive ADX bars to check", "Indicators")
			.SetNotNegative();

		_adxMinimum = Param(nameof(AdxMinimum), 20)
			.SetDisplay("ADX Minimum", "Minimum ADX strength", "Indicators")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 10)
			.SetDisplay("MACD Fast EMA", "Fast EMA length for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow EMA", "Slow EMA length for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 8)
			.SetDisplay("MACD Signal EMA", "Signal EMA length for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdBarsInterval = Param(nameof(MacdBarsInterval), 4)
			.SetDisplay("MACD Bars Interval", "Consecutive MACD bars to confirm trend", "Indicators")
			.SetNotNegative();

		_macdMinimumPips = Param(nameof(MacdMinimumPips), 30)
			.SetDisplay("MACD Minimum (pips)", "Minimum MACD magnitude in pips", "Indicators")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");

		ResetState();
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

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		UpdatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_macd, _adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Trade == null)
			return;

		if (Position > 0 && trade.Order?.Direction == Sides.Buy)
		{
			_entryPrice = trade.Trade.Price;
			_halfTaken = false;
			SetupRiskLevels(true);
		}
		else if (Position < 0 && trade.Order?.Direction == Sides.Sell)
		{
			_entryPrice = trade.Trade.Price;
			_halfTaken = false;
			SetupRiskLevels(false);
		}
		else if (Position == 0)
		{
			ResetProtectionLevels();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			ResetProtectionLevels();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize == 0m)
			UpdatePipSize();

		ManagePosition(candle);

		if (!macdValue.IsFinal || !adxValue.IsFinal)
			return;

		if (_macd == null || _adx == null)
			return;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdData.Macd is not decimal macdMain || macdData.Signal is not decimal macdSignal)
			return;

		var adxData = (AverageDirectionalIndexValue)adxValue;
		if (adxData.MovingAverage is not decimal adxMain)
			return;

		UpdateMacdHistory(macdMain, macdSignal);
		UpdateAdxHistory(adxMain);

		if (!HasMacdHistory() || !HasAdxHistory())
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		if (Position != 0)
			return;

		var macdMagnitude = _pipSize > 0m && _pipSize != decimal.Zero
			? macdMain / _pipSize
			: macdMain;

		var buySignal = GenerateBuySignal(macdMagnitude, adxMain);
		var sellSignal = GenerateSellSignal(macdMagnitude, adxMain);

		if (buySignal)
		{
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			SellMarket(volume);
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			HandleLongPosition(candle);
		}
		else if (Position < 0)
		{
			HandleShortPosition(candle);
		}
	}

	private void HandleLongPosition(ICandleMessage candle)
	{
		if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			ResetProtectionLevels();
			return;
		}

		if (_takePrice > 0m && candle.HighPrice >= _takePrice)
		{
			if (TakeHalfProfit && !_halfTaken)
			{
				var half = GetHalfVolume();
				if (half > 0m)
				{
					SellMarket(half);
					_halfTaken = true;
					_takePrice = 0m;
				}
			}
			else if (!TakeHalfProfit)
			{
				SellMarket(Position);
				ResetProtectionLevels();
				return;
			}
		}

		UpdateLongTrailing(candle);
	}

	private void HandleShortPosition(ICandleMessage candle)
	{
		if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
		{
			BuyMarket(-Position);
			ResetProtectionLevels();
			return;
		}

		if (_takePrice > 0m && candle.LowPrice <= _takePrice)
		{
			if (TakeHalfProfit && !_halfTaken)
			{
				var half = GetHalfVolume();
				if (half > 0m)
				{
					BuyMarket(half);
					_halfTaken = true;
					_takePrice = 0m;
				}
			}
			else if (!TakeHalfProfit)
			{
				BuyMarket(-Position);
				ResetProtectionLevels();
				return;
			}
		}

		UpdateShortTrailing(candle);
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = Math.Max(0, TrailingStepPips) * _pipSize;
		var profitDistance = candle.ClosePrice - _entryPrice;

		if (profitDistance <= trailingDistance + trailingStep)
			return;

		var desiredStop = candle.ClosePrice - trailingDistance;
		if (desiredStop > _stopPrice)
			_stopPrice = desiredStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = Math.Max(0, TrailingStepPips) * _pipSize;
		var profitDistance = _entryPrice - candle.ClosePrice;

		if (profitDistance <= trailingDistance + trailingStep)
			return;

		var desiredStop = candle.ClosePrice + trailingDistance;
		if (_stopPrice == 0m || desiredStop < _stopPrice)
			_stopPrice = desiredStop;
	}

	private bool GenerateBuySignal(decimal macdMagnitude, decimal adxValue)
	{
		if (macdMagnitude < MacdMinimumPips)
			return false;

		if ((int)adxValue < AdxMinimum)
			return false;

		for (var i = 0; i < MacdBarsInterval; i++)
		{
			if (_macdMainHistory[i] < _macdMainHistory[i + 1])
				return false;

			if (_macdSignalHistory[i] < _macdSignalHistory[i + 1])
				return false;
		}

		for (var i = 0; i < AdxBarsInterval; i++)
		{
			if (_adxHistory[i] < _adxHistory[i + 1])
				return false;
		}

		return true;
	}

	private bool GenerateSellSignal(decimal macdMagnitude, decimal adxValue)
	{
		if (-macdMagnitude < MacdMinimumPips)
			return false;

		if ((int)adxValue < AdxMinimum)
			return false;

		for (var i = 0; i < MacdBarsInterval; i++)
		{
			if (_macdMainHistory[i] > _macdMainHistory[i + 1])
				return false;

			if (_macdSignalHistory[i] > _macdSignalHistory[i + 1])
				return false;
		}

		for (var i = 0; i < AdxBarsInterval; i++)
		{
			if (_adxHistory[i] > _adxHistory[i + 1])
				return false;
		}

		return true;
	}

	private void UpdateMacdHistory(decimal macdMain, decimal macdSignal)
	{
		var required = Math.Max(1, MacdBarsInterval + 1);
		if (_macdMainHistory.Length != required)
		{
			_macdMainHistory = new decimal[required];
			_macdSignalHistory = new decimal[required];
			_macdHistoryCount = 0;
		}

		for (var i = Math.Min(required - 1, Math.Max(0, _macdHistoryCount - 1)); i > 0; i--)
		{
			_macdMainHistory[i] = _macdMainHistory[i - 1];
			_macdSignalHistory[i] = _macdSignalHistory[i - 1];
		}

		_macdMainHistory[0] = macdMain;
		_macdSignalHistory[0] = macdSignal;

		if (_macdHistoryCount < required)
			_macdHistoryCount++;
	}

	private void UpdateAdxHistory(decimal adxValue)
	{
		var required = Math.Max(1, AdxBarsInterval + 1);
		if (_adxHistory.Length != required)
		{
			_adxHistory = new decimal[required];
			_adxHistoryCount = 0;
		}

		for (var i = Math.Min(required - 1, Math.Max(0, _adxHistoryCount - 1)); i > 0; i--)
		{
			_adxHistory[i] = _adxHistory[i - 1];
		}

		_adxHistory[0] = adxValue;

		if (_adxHistoryCount < required)
			_adxHistoryCount++;
	}

	private bool HasMacdHistory()
	{
		var required = Math.Max(1, MacdBarsInterval + 1);
		return _macdHistoryCount >= required;
	}

	private bool HasAdxHistory()
	{
		var required = Math.Max(1, AdxBarsInterval + 1);
		return _adxHistoryCount >= required;
	}

	private void SetupRiskLevels(bool isLong)
	{
		if (_pipSize <= 0m)
			return;

		_stopPrice = 0m;
		_takePrice = 0m;

		if (StopLossPips > 0)
		{
			var offset = StopLossPips * _pipSize;
			_stopPrice = isLong ? _entryPrice - offset : _entryPrice + offset;
		}

		if (TakeProfitPips > 0)
		{
			var offset = TakeProfitPips * _pipSize;
			_takePrice = isLong ? _entryPrice + offset : _entryPrice - offset;
		}
	}

	private void ResetProtectionLevels()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_halfTaken = false;
	}

	private decimal GetHalfVolume()
	{
		var current = Math.Abs(Position);
		if (current <= 0m)
			return 0m;

		var half = current / 2m;
		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			half = Math.Floor(half / step) * step;
			if (half <= 0m)
			half = step;
		}

		if (half > current)
		half = current;

		return half;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		_pipSize = step > 0m ? step : 0m;
	}

	private void ResetState()
	{
		_macdMainHistory = Array.Empty<decimal>();
		_macdSignalHistory = Array.Empty<decimal>();
		_adxHistory = Array.Empty<decimal>();
		_macdHistoryCount = 0;
		_adxHistoryCount = 0;
		ResetProtectionLevels();
	}
}

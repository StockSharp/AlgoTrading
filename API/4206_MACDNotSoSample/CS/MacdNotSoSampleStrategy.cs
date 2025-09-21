namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MacdNotSoSampleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<decimal> _macdOpenLevelPips;
	private readonly StrategyParam<decimal> _macdCloseLevelPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _requiredSecurityCode;

	private MovingAverageConvergenceDivergenceSignal? _macd;
	private ExponentialMovingAverage? _trendEma;

	private bool _hasPreviousValues;
	private decimal _previousMacd;
	private decimal _previousSignal;
	private decimal _previousEma;

	private decimal _pipSize;
	private decimal _macdOpenThreshold;
	private decimal _macdCloseThreshold;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	public MacdNotSoSampleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 47)
			.SetGreaterThanZero()
			.SetDisplay("Fast period", "Fast EMA length used by the MACD indicator.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 166)
			.SetGreaterThanZero()
			.SetDisplay("Slow period", "Slow EMA length used by the MACD indicator.", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Signal period", "Signal line length for the MACD indicator.", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA period", "Length of the EMA trend filter.", "Indicators");

		_macdOpenLevelPips = Param(nameof(MacdOpenLevelPips), 1m)
			.SetGreaterThanZero()
			.SetDisplay("MACD open level", "Minimum MACD magnitude in pips to open a position.", "Thresholds");

		_macdCloseLevelPips = Param(nameof(MacdCloseLevelPips), 3m)
			.SetGreaterThanZero()
			.SetDisplay("MACD close level", "Minimum MACD magnitude in pips to close an existing position.", "Thresholds");

		_takeProfitPips = Param(nameof(TakeProfitPips), 550m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit", "Take-profit distance expressed in pips.", "Risk management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 19m)
			.SetNotNegative()
			.SetDisplay("Trailing stop", "Trailing stop distance expressed in pips.", "Risk management");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Default volume used for market entries.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle type", "Timeframe processed by the strategy.", "General");

		_requiredSecurityCode = Param(nameof(RequiredSecurityCode), "EURUSD")
			.SetDisplay("Required symbol", "Security code expected by the legacy MetaTrader expert.", "General");
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	public decimal MacdOpenLevelPips
	{
		get => _macdOpenLevelPips.Value;
		set => _macdOpenLevelPips.Value = value;
	}

	public decimal MacdCloseLevelPips
	{
		get => _macdCloseLevelPips.Value;
		set => _macdCloseLevelPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public string RequiredSecurityCode
	{
		get => _requiredSecurityCode.Value;
		set => _requiredSecurityCode.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_trendEma = null;
		_hasPreviousValues = false;
		_previousMacd = 0m;
		_previousSignal = 0m;
		_previousEma = 0m;
		_pipSize = 0m;
		_macdOpenThreshold = 0m;
		_macdCloseThreshold = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align helper order volume with the configured trade size.

		var requiredCode = RequiredSecurityCode;
		if (!string.IsNullOrEmpty(requiredCode) && Security?.Code is string code && !string.Equals(code, requiredCode, StringComparison.OrdinalIgnoreCase))
		{
			LogWarning($"Configured security {code} does not match required code {requiredCode}. Strategy will stop to mimic MetaTrader checks.");
			Stop();
			return;
		}

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		_trendEma = new ExponentialMovingAverage { Length = TrendPeriod };

		_hasPreviousValues = false;
		_previousMacd = 0m;
		_previousSignal = 0m;
		_previousEma = 0m;

		_pipSize = CalculatePipSize();
		_macdOpenThreshold = ConvertPipsToPrice(MacdOpenLevelPips);
		_macdCloseThreshold = ConvertPipsToPrice(MacdCloseLevelPips);
		_takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		_trailingStopDistance = ConvertPipsToPrice(TrailingStopPips);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // The MetaTrader EA works on fully closed candles only.

		if (!macdValue.IsFinal || !emaValue.IsFinal)
			return; // Wait for final indicator values.

		if (_macd == null || _trendEma == null)
			return;

		if (!_macd.IsFormed || !_trendEma.IsFormed)
			return; // Do not trade until all indicators have enough data.

		if (!IsFormedAndOnlineAndAllowTrading())
			return; // Ensure the strategy is ready to send orders.

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdCurrent = macdData.Macd;
		var signalCurrent = macdData.Signal;
		var emaCurrent = emaValue.ToDecimal();

		if (!_hasPreviousValues)
		{
			_previousMacd = macdCurrent;
			_previousSignal = signalCurrent;
			_previousEma = emaCurrent;
			_hasPreviousValues = true;
			return;
		}

		var position = Position;

		if (position > 0m)
		{
			if (ShouldCloseLong(macdCurrent, signalCurrent))
			{
				SellMarket(position);
				ResetLongState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			if (TryExitLongByRisk(candle))
			{
				SellMarket(position);
				ResetLongState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (position < 0m)
		{
			if (ShouldCloseShort(macdCurrent, signalCurrent))
			{
				BuyMarket(Math.Abs(position));
				ResetShortState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			if (TryExitShortByRisk(candle))
			{
				BuyMarket(Math.Abs(position));
				ResetShortState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			UpdateShortTrailing(candle);
		}
		else
		{
			if (ShouldOpenLong(macdCurrent, signalCurrent, emaCurrent))
			{
				BuyMarket();
				BeginLongPosition(candle.ClosePrice);
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			if (ShouldOpenShort(macdCurrent, signalCurrent, emaCurrent))
			{
				SellMarket();
				BeginShortPosition(candle.ClosePrice);
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}
		}

		UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
	}

	private bool ShouldOpenLong(decimal macdCurrent, decimal signalCurrent, decimal emaCurrent)
	{
		var hasMacdStrength = Math.Abs(macdCurrent) > _macdOpenThreshold;
		var macdAboveSignal = macdCurrent > signalCurrent;
		var macdWasBelowSignal = _previousMacd < _previousSignal;
		var emaRising = emaCurrent > _previousEma;

		if (macdCurrent < 0m && macdAboveSignal && macdWasBelowSignal && hasMacdStrength && emaRising)
		{
			return true; // MACD crossed above the signal line below zero while the EMA trends higher.
		}

		return false;
	}

	private bool ShouldOpenShort(decimal macdCurrent, decimal signalCurrent, decimal emaCurrent)
	{
		var hasMacdStrength = macdCurrent > _macdOpenThreshold;
		var macdBelowSignal = macdCurrent < signalCurrent;
		var macdWasAboveSignal = _previousMacd > _previousSignal;
		var emaFalling = emaCurrent < _previousEma;

		if (macdCurrent > 0m && macdBelowSignal && macdWasAboveSignal && hasMacdStrength && emaFalling)
		{
			return true; // MACD crossed below the signal line above zero while the EMA trends lower.
		}

		return false;
	}

	private bool ShouldCloseLong(decimal macdCurrent, decimal signalCurrent)
	{
		var macdBelowSignal = macdCurrent < signalCurrent;
		var macdWasAboveSignal = _previousMacd > _previousSignal;

		if (macdCurrent > 0m && macdBelowSignal && macdWasAboveSignal && macdCurrent > _macdCloseThreshold)
		{
			return true; // Positive MACD crossed below the signal, matching the exit rule of the MetaTrader EA.
		}

		return false;
	}

	private bool ShouldCloseShort(decimal macdCurrent, decimal signalCurrent)
	{
		var macdAboveSignal = macdCurrent > signalCurrent;
		var macdWasBelowSignal = _previousMacd < _previousSignal;

		if (macdCurrent < 0m && macdAboveSignal && macdWasBelowSignal && Math.Abs(macdCurrent) > _macdCloseThreshold)
		{
			return true; // Negative MACD crossed above the signal with enough magnitude.
		}

		return false;
	}

	private bool TryExitLongByRisk(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
		{
			return false;
		}

		if (_takeProfitDistance > 0m && candle.HighPrice >= entry + _takeProfitDistance)
		{
			LogInfo($"Exit long by take-profit at {candle.HighPrice:F5}.");
			return true;
		}

		if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
		{
			LogInfo($"Exit long by trailing stop at {candle.LowPrice:F5}.");
			return true;
		}

		return false;
	}

	private bool TryExitShortByRisk(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
		{
			return false;
		}

		if (_takeProfitDistance > 0m && candle.LowPrice <= entry - _takeProfitDistance)
		{
			LogInfo($"Exit short by take-profit at {candle.LowPrice:F5}.");
			return true;
		}

		if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
		{
			LogInfo($"Exit short by trailing stop at {candle.HighPrice:F5}.");
			return true;
		}

		return false;
	}

	private void BeginLongPosition(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longTrailingStop = null;
	}

	private void BeginShortPosition(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortTrailingStop = null;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
		{
			return;
		}

		if (_trailingStopDistance <= 0m)
		{
			return;
		}

		var advance = candle.HighPrice - entry;
		if (advance <= _trailingStopDistance)
		{
			return; // Price has not moved far enough to activate the trailing stop.
		}

		var candidate = candle.HighPrice - _trailingStopDistance;
		if (_longTrailingStop is not decimal current || candidate > current)
		{
			_longTrailingStop = candidate;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
		{
			return;
		}

		if (_trailingStopDistance <= 0m)
		{
			return;
		}

		var advance = entry - candle.LowPrice;
		if (advance <= _trailingStopDistance)
		{
			return; // Price has not moved far enough to activate the trailing stop.
		}

		var candidate = candle.LowPrice + _trailingStopDistance;
		if (_shortTrailingStop is not decimal current || candidate < current)
		{
			_shortTrailingStop = candidate;
		}
	}

	private void UpdatePreviousValues(decimal macdCurrent, decimal signalCurrent, decimal emaCurrent)
	{
		_previousMacd = macdCurrent;
		_previousSignal = signalCurrent;
		_previousEma = emaCurrent;
	}

	private decimal ConvertPipsToPrice(decimal value)
	{
		if (value <= 0m)
		{
			return 0m;
		}

		if (_pipSize > 0m)
		{
			return value * _pipSize;
		}

		return value;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		{
			return 0m;
		}

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return 0m;
		}

		var scale = (decimal.GetBits(step)[3] >> 16) & 0x7F;
		if (scale == 3 || scale == 5)
		{
			return step * 10m; // Convert 0.001 or 0.00001 steps to standard pip values.
		}

		return step;
	}
}

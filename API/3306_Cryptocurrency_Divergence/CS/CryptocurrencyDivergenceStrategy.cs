using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cryptocurrency divergence strategy using RSI swings confirmed by SMA and MACD filters.
/// </summary>
public class CryptocurrencyDivergenceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBullishLevel;
	private readonly StrategyParam<decimal> _rsiBearishLevel;
	private readonly StrategyParam<int> _macdShortLength;
	private readonly StrategyParam<int> _macdLongLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailDistance;

	private decimal? _previousHighPrice;
	private decimal? _previousHighRsi;
	private decimal? _currentHighPrice;
	private decimal? _currentHighRsi;
	private decimal? _previousLowPrice;
	private decimal? _previousLowRsi;
	private decimal? _currentLowPrice;
	private decimal? _currentLowRsi;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopPrice;
	private bool _breakEvenActivated;

	public CryptocurrencyDivergenceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Volume used for market orders", "Risk")
			.SetCanOptimize(true);

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetDisplay("Fast SMA", "Length of the fast moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetDisplay("Slow SMA", "Length of the slow moving average", "Indicators")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Period used to compute RSI", "Indicators")
			.SetCanOptimize(true);

		_rsiBullishLevel = Param(nameof(RsiBullishLevel), 45m)
			.SetDisplay("RSI Bullish Level", "Maximum RSI value considered oversold", "Indicators")
			.SetCanOptimize(true);

		_rsiBearishLevel = Param(nameof(RsiBearishLevel), 55m)
			.SetDisplay("RSI Bearish Level", "Minimum RSI value considered overbought", "Indicators")
			.SetCanOptimize(true);

		_macdShortLength = Param(nameof(MacdShortLength), 12)
			.SetDisplay("MACD Fast", "Short period of MACD", "Indicators")
			.SetCanOptimize(true);

		_macdLongLength = Param(nameof(MacdLongLength), 26)
			.SetDisplay("MACD Slow", "Long period of MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal period of MACD", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetDisplay("Stop Loss (steps)", "Stop loss distance measured in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit (steps)", "Take profit distance measured in price steps", "Risk")
			.SetCanOptimize(true);

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Enable Break-Even", "Move stop loss to break-even after profit", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 30m)
			.SetDisplay("Break-Even Trigger", "Profit distance required to move stop", "Risk")
			.SetCanOptimize(true);

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 5m)
			.SetDisplay("Break-Even Offset", "Offset added to entry price when moving stop", "Risk")
			.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activate trailing stop management", "Risk");

		_trailDistance = Param(nameof(TrailDistance), 40m)
			.SetDisplay("Trail Distance", "Trailing stop distance in price steps", "Risk")
			.SetCanOptimize(true);
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiBullishLevel { get => _rsiBullishLevel.Value; set => _rsiBullishLevel.Value = value; }
	public decimal RsiBearishLevel { get => _rsiBearishLevel.Value; set => _rsiBearishLevel.Value = value; }
	public int MacdShortLength { get => _macdShortLength.Value; set => _macdShortLength.Value = value; }
	public int MacdLongLength { get => _macdLongLength.Value; set => _macdLongLength.Value = value; }
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public bool EnableBreakEven { get => _enableBreakEven.Value; set => _enableBreakEven.Value = value; }
	public decimal BreakEvenTrigger { get => _breakEvenTrigger.Value; set => _breakEvenTrigger.Value = value; }
	public decimal BreakEvenOffset { get => _breakEvenOffset.Value; set => _breakEvenOffset.Value = value; }
	public bool EnableTrailing { get => _enableTrailing.Value; set => _enableTrailing.Value = value; }
	public decimal TrailDistance { get => _trailDistance.Value; set => _trailDistance.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousHighPrice = null;
		_previousHighRsi = null;
		_currentHighPrice = null;
		_currentHighRsi = null;
		_previousLowPrice = null;
		_previousLowRsi = null;
		_currentLowPrice = null;
		_currentLowRsi = null;

		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
		_breakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		StartProtection();

		var fastSma = new SimpleMovingAverage { Length = FastMaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdShortLength,
			LongPeriod = MacdLongLength,
			SignalPeriod = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastSma, slowSma, rsi, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawIndicator(area, rsi);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !rsiValue.IsFinal || !macdValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
		return;

		var newLow = TryUpdateLow(candle.LowPrice, rsi);
		var newHigh = TryUpdateHigh(candle.HighPrice, rsi);

		ManageLongPosition(candle);
		ManageShortPosition(candle);

		if (newLow)
		TryEnterLong(candle, fast, slow, rsi, macdLine, signalLine);

		if (newHigh)
		TryEnterShort(candle, fast, slow, rsi, macdLine, signalLine);
	}

	private bool TryUpdateLow(decimal low, decimal rsi)
	{
		if (_currentLowPrice == null || low < _currentLowPrice)
		{
			_previousLowPrice = _currentLowPrice;
			_previousLowRsi = _currentLowRsi;
			_currentLowPrice = low;
			_currentLowRsi = rsi;
			return _previousLowPrice != null && _previousLowRsi != null;
		}

		return false;
	}

	private bool TryUpdateHigh(decimal high, decimal rsi)
	{
		if (_currentHighPrice == null || high > _currentHighPrice)
		{
			_previousHighPrice = _currentHighPrice;
			_previousHighRsi = _currentHighRsi;
			_currentHighPrice = high;
			_currentHighRsi = rsi;
			return _previousHighPrice != null && _previousHighRsi != null;
		}

		return false;
	}

	private void TryEnterLong(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal macdLine, decimal signalLine)
	{
		if (_previousLowPrice == null || _previousLowRsi == null || _currentLowPrice == null || _currentLowRsi == null)
		return;

		if (_currentLowPrice >= _previousLowPrice)
		return;

		if (_currentLowRsi <= _previousLowRsi)
		return;

		if (fast <= slow)
		return;

		if (macdLine <= signalLine)
		return;

		if (rsi >= RsiBullishLevel)
		return;

		var requiredVolume = TradeVolume;
		if (Position < 0)
		requiredVolume += Math.Abs(Position);

		if (requiredVolume <= 0)
		return;

		BuyMarket(requiredVolume);
		LogInfo($"Enter long on bullish divergence at {candle.ClosePrice}");
	}

	private void TryEnterShort(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal macdLine, decimal signalLine)
	{
		if (_previousHighPrice == null || _previousHighRsi == null || _currentHighPrice == null || _currentHighRsi == null)
		return;

		if (_currentHighPrice <= _previousHighPrice)
		return;

		if (_currentHighRsi >= _previousHighRsi)
		return;

		if (fast >= slow)
		return;

		if (macdLine >= signalLine)
		return;

		if (rsi <= RsiBearishLevel)
		return;

		var requiredVolume = TradeVolume;
		if (Position > 0)
		requiredVolume += Math.Abs(Position);

		if (requiredVolume <= 0)
		return;

		SellMarket(requiredVolume);
		LogInfo($"Enter short on bearish divergence at {candle.ClosePrice}");
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
		return;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0)
		return;

		if (_enableTrailing.Value && TrailDistance > 0)
		{
			var newTrailing = candle.ClosePrice - step * TrailDistance;
			_trailingStopPrice = _trailingStopPrice.HasValue ? Math.Max(_trailingStopPrice.Value, newTrailing) : newTrailing;
		}

		if (_enableBreakEven.Value && !_breakEvenActivated && BreakEvenTrigger > 0 && _entryPrice.HasValue)
		{
			var profitDistance = candle.ClosePrice - _entryPrice.Value;
			if (profitDistance >= step * BreakEvenTrigger)
			{
				_stopLossPrice = _entryPrice.Value + step * BreakEvenOffset;
				_breakEvenActivated = true;
				LogInfo("Move long stop to break-even zone");
			}
		}

		var exitPrice = _trailingStopPrice ?? _stopLossPrice;
		if (exitPrice.HasValue && candle.LowPrice <= exitPrice.Value)
		{
			SellMarket(Position);
			LogInfo($"Exit long via protective stop at {exitPrice}");
			return;
		}

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			SellMarket(Position);
			LogInfo($"Exit long via take profit at {_takeProfitPrice}");
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
		return;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0)
		return;

		if (_enableTrailing.Value && TrailDistance > 0)
		{
			var newTrailing = candle.ClosePrice + step * TrailDistance;
			_trailingStopPrice = _trailingStopPrice.HasValue ? Math.Min(_trailingStopPrice.Value, newTrailing) : newTrailing;
		}

		if (_enableBreakEven.Value && !_breakEvenActivated && BreakEvenTrigger > 0 && _entryPrice.HasValue)
		{
			var profitDistance = _entryPrice.Value - candle.ClosePrice;
			if (profitDistance >= step * BreakEvenTrigger)
			{
				_stopLossPrice = _entryPrice.Value - step * BreakEvenOffset;
				_breakEvenActivated = true;
				LogInfo("Move short stop to break-even zone");
			}
		}

		var exitPrice = _trailingStopPrice ?? _stopLossPrice;
		if (exitPrice.HasValue && candle.HighPrice >= exitPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short via protective stop at {exitPrice}");
			return;
		}

		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short via take profit at {_takeProfitPrice}");
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
		return;

		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0)
			{
				SetupLongRisk(trade.Trade.Price);
			}
			else if (Position == 0)
			{
				ResetRisk();
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0)
			{
				SetupShortRisk(trade.Trade.Price);
			}
			else if (Position == 0)
			{
				ResetRisk();
			}
		}
	}

	private void SetupLongRisk(decimal price)
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0)
		return;

		_entryPrice = price;
		_stopLossPrice = price - step * StopLossPoints;
		_takeProfitPrice = price + step * TakeProfitPoints;
		_trailingStopPrice = null;
		_breakEvenActivated = false;
	}

	private void SetupShortRisk(decimal price)
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0)
		return;

		_entryPrice = price;
		_stopLossPrice = price + step * StopLossPoints;
		_takeProfitPrice = price - step * TakeProfitPoints;
		_trailingStopPrice = null;
		_breakEvenActivated = false;
	}

	private void ResetRisk()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
		_breakEvenActivated = false;
	}
}


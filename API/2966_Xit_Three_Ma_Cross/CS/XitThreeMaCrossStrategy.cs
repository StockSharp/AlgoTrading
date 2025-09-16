using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy with MACD momentum and ATR-based risk management.
/// </summary>
public class XitThreeMaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _intermediateMaLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _slowMaType;
	private readonly StrategyParam<MovingAverageTypeEnum> _intermediateMaType;
	private readonly StrategyParam<MovingAverageTypeEnum> _fastMaType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _macdTriggerPoints;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _atrTakeCandleType;
	private readonly StrategyParam<DataType> _atrStopCandleType;
	private readonly StrategyParam<decimal> _riskPercent;

	private decimal _slowPrev;
	private decimal _intermediatePrev;
	private decimal _fastPrev;
	private decimal _macdPrev;
	private decimal _signalPrev;
	private bool _hasPrev;

	private decimal _atrTakeValue;
	private decimal _atrStopValue;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfit;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int IntermediateMaLength { get => _intermediateMaLength.Value; set => _intermediateMaLength.Value = value; }
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public MovingAverageTypeEnum SlowMaType { get => _slowMaType.Value; set => _slowMaType.Value = value; }
	public MovingAverageTypeEnum IntermediateMaType { get => _intermediateMaType.Value; set => _intermediateMaType.Value = value; }
	public MovingAverageTypeEnum FastMaType { get => _fastMaType.Value; set => _fastMaType.Value = value; }
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }
	public decimal MacdTriggerPoints { get => _macdTriggerPoints.Value; set => _macdTriggerPoints.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public DataType AtrTakeCandleType { get => _atrTakeCandleType.Value; set => _atrTakeCandleType.Value = value; }
	public DataType AtrStopCandleType { get => _atrStopCandleType.Value; set => _atrStopCandleType.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	public XitThreeMaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_slowMaLength = Param(nameof(SlowMaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of slow trend filter", "Indicators");

		_intermediateMaLength = Param(nameof(IntermediateMaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Intermediate MA Length", "Length of trend confirmation MA", "Indicators");

		_fastMaLength = Param(nameof(FastMaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of trigger MA", "Indicators");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Slow MA Type", "Type of slow moving average", "Indicators");

		_intermediateMaType = Param(nameof(IntermediateMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Intermediate MA Type", "Type of intermediate moving average", "Indicators");

		_fastMaType = Param(nameof(FastMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Fast MA Type", "Type of fast moving average", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing length", "Indicators");

		_macdTriggerPoints = Param(nameof(MacdTriggerPoints), 7m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Trigger Points", "Distance in points between MACD and signal", "Filters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for risk levels", "Risk");

		_atrTakeCandleType = Param(nameof(AtrTakeCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("ATR Take Profit Type", "Timeframe for take profit ATR", "Risk");

		_atrStopCandleType = Param(nameof(AtrStopCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("ATR Stop Type", "Timeframe for stop loss ATR", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of portfolio risked per trade", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		if (CandleType != null)
			yield return (Security, CandleType);

		if (AtrTakeCandleType != null && AtrTakeCandleType != CandleType)
			yield return (Security, AtrTakeCandleType);

		if (AtrStopCandleType != null && AtrStopCandleType != CandleType && AtrStopCandleType != AtrTakeCandleType)
			yield return (Security, AtrStopCandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_hasPrev = false;
		_slowPrev = 0m;
		_intermediatePrev = 0m;
		_fastPrev = 0m;
		_macdPrev = 0m;
		_signalPrev = 0m;
		_atrTakeValue = 0m;
		_atrStopValue = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var slowMa = CreateMovingAverage(SlowMaType, SlowMaLength);
		var intermediateMa = CreateMovingAverage(IntermediateMaType, IntermediateMaLength);
		var fastMa = CreateMovingAverage(FastMaType, FastMaLength);
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortLength = MacdFastLength,
			LongLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, slowMa, intermediateMa, fastMa, ProcessSignal).Start();

		var atrTake = new AverageTrueRange { Length = AtrLength };
		var atrStop = new AverageTrueRange { Length = AtrLength };

		SubscribeCandles(AtrTakeCandleType).Bind(atrTake, UpdateAtrTake).Start();
		SubscribeCandles(AtrStopCandleType).Bind(atrStop, UpdateAtrStop).Start();
	}

	private void ProcessSignal(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue slowValue, IIndicatorValue intermediateValue, IIndicatorValue fastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!macdValue.IsFinal || !slowValue.IsFinal || !intermediateValue.IsFinal || !fastValue.IsFinal)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			return;

		var slowMa = slowValue.ToDecimal();
		var intermediateMa = intermediateValue.ToDecimal();
		var fastMa = fastValue.ToDecimal();

		if (!_hasPrev)
		{
			UpdatePreviousValues(slowMa, intermediateMa, fastMa, macdLine, signalLine);
			return;
		}

		if (TryClosePosition(candle.ClosePrice, fastMa, intermediateMa))
		{
			UpdatePreviousValues(slowMa, intermediateMa, fastMa, macdLine, signalLine);
			return;
		}

		if (_atrStopValue <= 0m || _atrTakeValue <= 0m)
		{
			UpdatePreviousValues(slowMa, intermediateMa, fastMa, macdLine, signalLine);
			return;
		}

		var macdTrigger = GetMacdTrigger();
		var bullishMomentum = macdLine > _macdPrev && signalLine > _signalPrev && macdLine - signalLine > macdTrigger;
		var bearishMomentum = macdLine < _macdPrev && signalLine < _signalPrev && signalLine - macdLine > macdTrigger;

		var bullishTrend = intermediateMa > _intermediatePrev && fastMa > _fastPrev && intermediateMa > slowMa && fastMa > intermediateMa;
		var bearishTrend = intermediateMa < _intermediatePrev && fastMa < _fastPrev && intermediateMa < slowMa && fastMa < intermediateMa;

		if (Position <= 0 && bullishMomentum && bullishTrend)
		{
			var volume = CalculatePositionSize(_atrStopValue);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - _atrStopValue;
				_takeProfit = _entryPrice + _atrTakeValue;
			}
		}
		else if (Position >= 0 && bearishMomentum && bearishTrend)
		{
			var volume = CalculatePositionSize(_atrStopValue);
			if (volume > 0m)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + _atrStopValue;
				_takeProfit = _entryPrice - _atrTakeValue;
			}
		}

		UpdatePreviousValues(slowMa, intermediateMa, fastMa, macdLine, signalLine);
	}

	private bool TryClosePosition(decimal closePrice, decimal fastMa, decimal intermediateMa)
	{
		if (Position > 0)
		{
			var exitByMa = fastMa < intermediateMa;
			var stopHit = _stopPrice > 0m && closePrice <= _stopPrice;
			var targetHit = _takeProfit > 0m && closePrice >= _takeProfit;

			if (exitByMa || stopHit || targetHit)
			{
				SellMarket(Position);
				ResetTradeLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			var exitByMa = fastMa > intermediateMa;
			var stopHit = _stopPrice > 0m && closePrice >= _stopPrice;
			var targetHit = _takeProfit > 0m && closePrice <= _takeProfit;

			if (exitByMa || stopHit || targetHit)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeLevels();
				return true;
			}
		}

		return false;
	}

	private void UpdateAtrTake(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_atrTakeValue = atr;
	}

	private void UpdateAtrStop(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_atrStopValue = atr;
	}

	private void UpdatePreviousValues(decimal slowMa, decimal intermediateMa, decimal fastMa, decimal macdLine, decimal signalLine)
	{
		_slowPrev = slowMa;
		_intermediatePrev = intermediateMa;
		_fastPrev = fastMa;
		_macdPrev = macdLine;
		_signalPrev = signalLine;
		_hasPrev = true;
	}

	private decimal CalculatePositionSize(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return Volume;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? 0m;
		if (portfolioValue <= 0m || RiskPercent <= 0m)
			return Volume;

		var step = Security?.PriceStep ?? 0m;
		var stepCost = Security?.PriceStepCost ?? 0m;

		decimal riskPerUnit;
		if (step > 0m && stepCost > 0m)
		{
			var ticks = stopDistance / step;
			riskPerUnit = ticks * stepCost;
		}
		else
		{
			riskPerUnit = stopDistance;
		}

		if (riskPerUnit <= 0m)
			return Volume;

		var allowedRisk = portfolioValue * RiskPercent / 100m;
		if (allowedRisk <= 0m)
			return Volume;

		var rawVolume = allowedRisk / riskPerUnit;
		var volumeStep = Security?.VolumeStep ?? 0m;

		if (volumeStep > 0m)
		{
			rawVolume = Math.Floor(rawVolume / volumeStep) * volumeStep;
			if (rawVolume <= 0m)
				rawVolume = volumeStep;
		}

		return rawVolume > 0m ? rawVolume : Volume;
	}

	private void ResetTradeLevels()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfit = 0m;
	}

	private decimal GetMacdTrigger()
	{
		var step = Security?.PriceStep ?? 1m;
		return MacdTriggerPoints * step;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SMA { Length = length },
			MovingAverageTypeEnum.Exponential => new EMA { Length = length },
			MovingAverageTypeEnum.Smoothed => new SMMA { Length = length },
			MovingAverageTypeEnum.Weighted => new WMA { Length = length },
			_ => new SMA { Length = length }
		};
	}

	public enum MovingAverageTypeEnum
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}

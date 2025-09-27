namespace StockSharp.Samples.Strategies;

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

public class BollingerRsiMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<bool> _useAutoLot;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _fixedVolume;

	private BollingerBands _bollingerBands;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _dailyEma;

	private decimal? _latestDailyEma;
	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;

	public BollingerRsiMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe used for Bollinger Bands and RSI.", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle Type", "Higher timeframe used by the EMA filter.", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of candles used by Bollinger Bands.", "Bollinger Bands");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Width multiplier applied to the Bollinger Bands.", "Bollinger Bands");

		_rsiPeriod = Param(nameof(RsiPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of candles used by the RSI oscillator.", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Upper", "Overbought threshold that validates short trades.", "RSI");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Lower", "Oversold threshold that validates long trades.", "RSI");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the higher timeframe EMA filter.", "EMA");

		_stopLossOffset = Param(nameof(StopLossOffset), 0.0238m)
			.SetNotNegative()
			.SetDisplay("Stop Offset", "Extra price distance applied beyond the Bollinger Band for the stop-loss.", "Risk");

		_useAutoLot = Param(nameof(UseAutoLot), true)
			.SetDisplay("Use Auto Lot", "Size positions using the risk percentage.", "Money Management");

		_riskPerTrade = Param(nameof(RiskPerTrade), 0.05m)
			.SetRange(0m, 1m)
			.SetDisplay("Risk Per Trade", "Fraction of equity allocated to each trade when auto lot is enabled.", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Order volume used when auto lot sizing is disabled.", "Money Management");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	public bool UseAutoLot
	{
		get => _useAutoLot.Value;
		set => _useAutoLot.Value = value;
	}

	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_latestDailyEma = null;
		_longStopLoss = null;
		_longTakeProfit = null;
		_shortStopLoss = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollingerBands = new BollingerBands
		{
			Length = Math.Max(1, BollingerPeriod),
			Width = BollingerDeviation,
			CandlePrice = CandlePrice.Close
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = Math.Max(1, RsiPeriod)
		};

		_dailyEma = new ExponentialMovingAverage
		{
			Length = Math.Max(1, MaPeriod)
		};

		Volume = AdjustVolume(FixedVolume);

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
			.Bind(_bollingerBands, _rsi, ProcessTradingCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _bollingerBands);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (_dailyEma == null)
			return;

		var value = _dailyEma.Process(candle.ClosePrice, candle.OpenTime, candle.State == CandleStates.Finished).ToNullableDecimal();
		if (candle.State != CandleStates.Finished)
			return;

		if (value == null)
			return;

		_latestDailyEma = value;
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bollingerBands == null || _rsi == null)
			return;

		if (!_bollingerBands.IsFormed || !_rsi.IsFormed)
			return;

		if (_latestDailyEma is not decimal emaValue)
			return;

		if (TryCloseByTargets(candle))
			return;

		if (Position != 0)
			return;

		var targetVolume = CalculateTradeVolume(candle.ClosePrice);
		if (targetVolume <= 0m)
			return;

		Volume = targetVolume;

		var longCondition = candle.ClosePrice < lowerBand && rsiValue < RsiLowerLevel && candle.ClosePrice > emaValue;
		var shortCondition = candle.ClosePrice > upperBand && rsiValue > RsiUpperLevel && candle.ClosePrice < emaValue;

		if (longCondition)
		{
			BuyMarket(Volume);
			_longStopLoss = lowerBand - StopLossOffset;
			_longTakeProfit = middleBand;
			_shortStopLoss = null;
			_shortTakeProfit = null;
		}
		else if (shortCondition)
		{
			SellMarket(Volume);
			_shortStopLoss = upperBand + StopLossOffset;
			_shortTakeProfit = middleBand;
			_longStopLoss = null;
			_longTakeProfit = null;
		}
	}

	private bool TryCloseByTargets(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStopLoss is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				_longStopLoss = null;
				_longTakeProfit = null;
				return true;
			}

			if (_longTakeProfit is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(Math.Abs(Position));
				_longStopLoss = null;
				_longTakeProfit = null;
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStopLoss is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopLoss = null;
				_shortTakeProfit = null;
				return true;
			}

			if (_shortTakeProfit is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopLoss = null;
				_shortTakeProfit = null;
				return true;
			}
		}

		return false;
	}

	private decimal CalculateTradeVolume(decimal referencePrice)
	{
		if (!UseAutoLot)
			return AdjustVolume(FixedVolume);

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
			return AdjustVolume(FixedVolume);

		if (referencePrice <= 0m || StopLossOffset <= 0m)
			return AdjustVolume(FixedVolume);

		var contractSize = Security?.LotSize ?? 1m;
		if (contractSize <= 0m)
			contractSize = 1m;

		var riskAmount = portfolioValue * RiskPerTrade;
		if (riskAmount <= 0m)
			return AdjustVolume(FixedVolume);

		var stopDistance = StopLossOffset * referencePrice;
		if (stopDistance <= 0m)
			return AdjustVolume(FixedVolume);

		var rawVolume = riskAmount / (stopDistance * contractSize);
		return AdjustVolume(rawVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;

		if (security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume is decimal min && min > 0m && volume < min)
			volume = min;

		if (security?.MaxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}
}


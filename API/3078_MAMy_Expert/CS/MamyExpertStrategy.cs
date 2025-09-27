
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

using StockSharp.Algo;

public class MamyExpertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MaCalculationTypes> _maType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private LengthIndicator<decimal> _closeMa;
	private LengthIndicator<decimal> _openMa;
	private LengthIndicator<decimal> _weightedPriceMa;

	private decimal? _previousCloseMa;
	private decimal? _previousOpenMa;
	private decimal? _previousWeightedMa;
	private decimal? _previousOpenSignal;
	private decimal? _previousCloseSignal;

	public MamyExpertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for price aggregation.", "General");

		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Length applied to all moving averages.", "Indicator");

		_maType = Param(nameof(MaType), MaCalculationTypes.Weighted)
			.SetDisplay("MA method", "Averaging algorithm applied to open/close/weighted prices.", "Indicator");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume submitted for entries.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public MaCalculationTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCloseMa = null;
		_previousOpenMa = null;
		_previousWeightedMa = null;
		_previousOpenSignal = null;
		_previousCloseSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		StartProtection();

		_closeMa = CreateMovingAverage(MaType, MaPeriod);
		_openMa = CreateMovingAverage(MaType, MaPeriod);
		_weightedPriceMa = CreateMovingAverage(MaType, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeMa);
			DrawIndicator(area, _openMa);
			DrawIndicator(area, _weightedPriceMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_closeMa == null || _openMa == null || _weightedPriceMa == null)
			return;

		var closeMaValue = _closeMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var openMaValue = _openMa.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		var weightedPrice = CalculateWeightedPrice(candle);
		var weightedMaValue = _weightedPriceMa.Process(weightedPrice, candle.OpenTime, true).ToDecimal();

		var previousCloseMa = _previousCloseMa;
		var previousOpenMa = _previousOpenMa;
		var previousWeightedMa = _previousWeightedMa;
		var previousOpenSignal = _previousOpenSignal;
		var previousCloseSignal = _previousCloseSignal;

		_previousCloseMa = closeMaValue;
		_previousOpenMa = openMaValue;
		_previousWeightedMa = weightedMaValue;

		if (!_closeMa.IsFormed || !_openMa.IsFormed || !_weightedPriceMa.IsFormed)
		{
			_previousOpenSignal = null;
			_previousCloseSignal = null;
			return;
		}

		var closeSignal = closeMaValue - weightedMaValue;
		var openSignal = 0m;

		if (previousCloseMa.HasValue && previousOpenMa.HasValue && previousWeightedMa.HasValue && previousCloseSignal.HasValue)
		{
			var closeDecreasing = closeMaValue < previousCloseMa.Value &&
				weightedMaValue < previousWeightedMa.Value &&
				closeMaValue < weightedMaValue &&
				weightedMaValue < openMaValue &&
				previousWeightedMa.Value < previousOpenMa.Value &&
				closeSignal <= previousCloseSignal.Value;

			var closeIncreasing = closeMaValue > previousCloseMa.Value &&
				weightedMaValue > previousWeightedMa.Value &&
				closeMaValue > weightedMaValue &&
				weightedMaValue > openMaValue &&
				previousWeightedMa.Value > previousOpenMa.Value &&
				closeSignal >= previousCloseSignal.Value;

			if (closeDecreasing || closeIncreasing)
				openSignal = (weightedMaValue - openMaValue) + (closeMaValue - weightedMaValue);
		}

		if (previousOpenSignal.HasValue && previousCloseSignal.HasValue &&
			openSignal >= 0m &&
			openSignal > previousOpenSignal.Value &&
			closeSignal < 0m &&
			previousCloseSignal.Value >= 0m)
		{
			closeSignal = 0m;
		}

		var hasPreviousOpenSignal = previousOpenSignal.HasValue;
		var hasPreviousCloseSignal = previousCloseSignal.HasValue;

		var openBuy = hasPreviousOpenSignal && openSignal > 0m && previousOpenSignal.Value <= 0m;
		var openSell = hasPreviousOpenSignal && openSignal < 0m && previousOpenSignal.Value >= 0m;
		var closeBuy = hasPreviousCloseSignal && closeSignal < 0m && previousCloseSignal.Value >= 0m;
		var closeSell = hasPreviousCloseSignal && closeSignal > 0m && previousCloseSignal.Value <= 0m;

		_previousOpenSignal = openSignal;
		_previousCloseSignal = closeSignal;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (TradeVolume <= 0m)
			return;

		var longPosition = Position > 0m ? Position : 0m;
		var shortPosition = Position < 0m ? -Position : 0m;

		if (longPosition > 0m)
		{
			if (closeBuy)
				SellMarket(longPosition);
		}
		else if (shortPosition > 0m)
		{
			if (closeSell)
				BuyMarket(shortPosition);
		}
		else
		{
			if (openBuy)
				BuyMarket(TradeVolume);
			else if (openSell)
				SellMarket(TradeVolume);
		}
	}

	private static decimal CalculateWeightedPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaCalculationTypes type, int length)
	{
		return type switch
		{
			MaCalculationTypes.Simple => new SimpleMovingAverage { Length = length },
			MaCalculationTypes.Exponential => new ExponentialMovingAverage { Length = length },
			MaCalculationTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length },
		};
	}
}

public enum MaCalculationTypes
{
	Simple,
	Exponential,
	Smoothed,
	Weighted
}


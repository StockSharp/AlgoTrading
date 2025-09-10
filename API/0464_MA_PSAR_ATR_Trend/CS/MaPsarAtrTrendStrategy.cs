using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average crossover with Parabolic SAR filter and ATR stop.
/// </summary>
public class MaPsarAtrTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageTypeEnum> _fastMaType;
	private readonly StrategyParam<MovingAverageTypeEnum> _slowMaType;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplierLong;
	private readonly StrategyParam<decimal> _atrMultiplierShort;
	private readonly StrategyParam<bool> _usePsarFilter;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _psarValue;
	private decimal _stopPrice;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast MA type.
	/// </summary>
	public MovingAverageTypeEnum FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Slow MA type.
	/// </summary>
	public MovingAverageTypeEnum SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for long stop.
	/// </summary>
	public decimal AtrMultiplierLong
	{
		get => _atrMultiplierLong.Value;
		set => _atrMultiplierLong.Value = value;
	}

	/// <summary>
	/// ATR multiplier for short stop.
	/// </summary>
	public decimal AtrMultiplierShort
	{
		get => _atrMultiplierShort.Value;
		set => _atrMultiplierShort.Value = value;
	}

	/// <summary>
	/// Use daily Parabolic SAR filter.
	/// </summary>
	public bool UsePsarFilter
	{
		get => _usePsarFilter.Value;
		set => _usePsarFilter.Value = value;
	}

	/// <summary>
	/// Candle type for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MaPsarAtrTrendStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 40)
			.SetDisplay("Fast MA Period", "Period for fast moving average", "MA")
			.SetGreaterThanZero();

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 160)
			.SetDisplay("Slow MA Period", "Period for slow moving average", "MA")
			.SetGreaterThanZero();

		_fastMaType = Param(nameof(FastMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Fast MA Type", "Type of fast moving average", "MA");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Slow MA Type", "Type of slow moving average", "MA");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Parabolic SAR");

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetDisplay("SAR Max Step", "Maximum acceleration factor for Parabolic SAR", "Parabolic SAR");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "ATR")
			.SetGreaterThanZero();

		_atrMultiplierLong = Param(nameof(AtrMultiplierLong), 2m)
			.SetDisplay("ATR Multiplier Long", "ATR multiplier for long stop", "Risk");

		_atrMultiplierShort = Param(nameof(AtrMultiplierShort), 2m)
			.SetDisplay("ATR Multiplier Short", "ATR multiplier for short stop", "Risk");

		_usePsarFilter = Param(nameof(UsePsarFilter), true)
			.SetDisplay("Use PSAR Filter", "Enable Parabolic SAR filter", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return UsePsarFilter
			? [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())]
			: [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_psarValue = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = CreateMa(FastMaType, FastMaPeriod);
		var slowMa = CreateMa(SlowMaType, SlowMaPeriod);
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var tradeSub = SubscribeCandles(CandleType);
		tradeSub
			.Bind(fastMa, slowMa, atr, ProcessCandle)
			.Start();

		if (UsePsarFilter)
		{
			var psar = new ParabolicSar
			{
				AccelerationStep = SarStep,
				AccelerationMax = SarMaxStep
			};

			var dailySub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
			dailySub
				.Bind(psar, ProcessDailySar)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSub);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Hull => new HullMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessDailySar(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_psarValue = sarValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var bullishTrend = fastValue > slowValue && price > fastValue;
		var bearishTrend = fastValue < slowValue && price < fastValue;

		var psarBull = !UsePsarFilter || candle.LowPrice > _psarValue;
		var psarBear = !UsePsarFilter || candle.HighPrice < _psarValue;

		if (Position > 0)
		{
			if (bearishTrend || price <= _stopPrice)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			if (bullishTrend || price >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		if (bullishTrend && psarBull && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = price - atrValue * AtrMultiplierLong;
		}
		else if (bearishTrend && psarBear && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = price + atrValue * AtrMultiplierShort;
		}
	}

	/// <summary>
	/// Moving average type enumeration.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Simple Moving Average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential Moving Average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Weighted Moving Average.
		/// </summary>
		Weighted,

		/// <summary>
		/// Smoothed Moving Average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Hull Moving Average.
		/// </summary>
		Hull
	}
}

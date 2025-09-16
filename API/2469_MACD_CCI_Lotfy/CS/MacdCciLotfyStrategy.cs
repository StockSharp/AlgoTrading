using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining MACD and CCI indicators.
/// Opens long when both indicators drop below negative threshold.
/// Opens short when both indicators rise above positive threshold.
/// </summary>
public class MacdCciLotfyStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _macdCoefficient;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to MACD value to match CCI scale.
	/// </summary>
	public decimal MacdCoefficient
	{
		get => _macdCoefficient.Value;
		set => _macdCoefficient.Value = value;
	}

	/// <summary>
	/// Absolute threshold level for signals.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type used in calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdCciLotfyStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI indicator", "General");

		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast EMA", "Fast EMA length for MACD", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 33)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow EMA", "Slow EMA length for MACD", "MACD");

		_macdCoefficient = Param(nameof(MacdCoefficient), 86000m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Coefficient", "Multiplier for MACD value", "MACD");

		_threshold = Param(nameof(Threshold), 85m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "Absolute level for signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			Fast = FastPeriod,
			Slow = SlowPeriod,
			Signal = 2
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdMain = macdData.Macd * MacdCoefficient;
		var cci = cciValue.ToDecimal();

		if (cci < -Threshold && macdMain < -Threshold)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (cci > Threshold && macdMain > Threshold)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}

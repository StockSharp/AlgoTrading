using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chande Momentum Oscillator strategy.
/// Long only: buys when CMO(CmoPeriod) is below LowerThreshold and closes the long when CMO rises above UpperThreshold or after
/// MaxBarsInPosition candles.
/// </summary>
public class ChandeMomentumOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<int> _maxBarsInPosition;
	private readonly StrategyParam<DataType> _candleType;

	private int _barsInPosition;

	/// <summary>
	/// CMO period.
	/// </summary>
	public int CmoPeriod
	{
		get => _cmoPeriod.Value;
		set => _cmoPeriod.Value = value;
	}

	/// <summary>
	/// CMO level below which the strategy buys.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// CMO level above which the long closes.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Candles a position is held at most.
	/// </summary>
	public int MaxBarsInPosition
	{
		get => _maxBarsInPosition.Value;
		set => _maxBarsInPosition.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ChandeMomentumOscillatorStrategy()
	{
		_cmoPeriod = Param(nameof(CmoPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("CMO Period", "CMO period", "CMO");

		_lowerThreshold = Param(nameof(LowerThreshold), -50m)
			.SetDisplay("Lower Threshold", "CMO level below which the strategy buys", "CMO");

		_upperThreshold = Param(nameof(UpperThreshold), 50m)
			.SetDisplay("Upper Threshold", "CMO level above which the long closes", "CMO");

		_maxBarsInPosition = Param(nameof(MaxBarsInPosition), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars In Position", "Candles a position is held at most", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsInPosition = 0;

		var cmo = new ChandeMomentumOscillator { Length = CmoPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cmo, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cmo);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position > 0)
			_barsInPosition++;

		if (!cmoValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cmo = cmoValue.GetValue<decimal>();

		if (Position > 0)
		{
			if (cmo > UpperThreshold || _barsInPosition >= MaxBarsInPosition)
				SellMarket(Position);

			return;
		}

		if (Position == 0 && cmo < LowerThreshold)
		{
			BuyMarket(Volume);
			_barsInPosition = 0;
		}
	}
}

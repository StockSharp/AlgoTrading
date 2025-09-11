using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Chande Momentum Oscillator.
/// Enters long when CMO drops below a lower threshold and exits
/// when CMO exceeds an upper threshold or after a fixed number of bars.
/// </summary>
public class ChandeMomentumOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<int> _maxBarsInPosition;
	private readonly StrategyParam<DataType> _candleType;

	private ChandeMomentumOscillator _cmo;
	private int _barsSinceEntry;

	/// <summary>
	/// Period for the Chande Momentum Oscillator.
	/// </summary>
	public int CmoPeriod
	{
		get => _cmoPeriod.Value;
		set => _cmoPeriod.Value = value;
	}

	/// <summary>
	/// Lower threshold to open a long position.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Upper threshold to close a long position.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of bars to hold a position.
	/// </summary>
	public int MaxBarsInPosition
	{
		get => _maxBarsInPosition.Value;
		set => _maxBarsInPosition.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ChandeMomentumOscillatorStrategy"/>.
	/// </summary>
	public ChandeMomentumOscillatorStrategy()
	{
		_cmoPeriod = Param(nameof(CmoPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("CMO Period", "Period for Chande Momentum Oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_lowerThreshold = Param(nameof(LowerThreshold), -50m)
			.SetDisplay("Lower Threshold", "Enter long when CMO is below", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(-70m, -30m, 5m);

		_upperThreshold = Param(nameof(UpperThreshold), 50m)
			.SetDisplay("Upper Threshold", "Exit long when CMO is above", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(30m, 70m, 5m);

		_maxBarsInPosition = Param(nameof(MaxBarsInPosition), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars In Position", "Exit after specified number of bars", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

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
		_barsSinceEntry = 0;
		_cmo = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cmo = new ChandeMomentumOscillator { Length = CmoPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cmo, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cmo);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cmo == null || !_cmo.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			_barsSinceEntry++;

			if (cmoValue > UpperThreshold || _barsSinceEntry >= MaxBarsInPosition)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long: CMO={cmoValue:F2}, Bars={_barsSinceEntry}");
				_barsSinceEntry = 0;
			}
		}
		else
		{
			_barsSinceEntry = 0;

			if (cmoValue < LowerThreshold)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Enter long: CMO={cmoValue:F2}");
			}
		}
	}
}

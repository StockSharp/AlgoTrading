using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "RAVIiAO" that combines the RAVI oscillator and the Acceleration/Deceleration oscillator.
/// </summary>
public class RaviIaoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private SMA _aoAverage;

	private decimal? _prevRavi;
	private decimal? _prevPrevRavi;
	private decimal? _prevAc;
	private decimal? _prevPrevAc;

	/// <summary>
	/// Type of candles used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average length for the RAVI oscillator.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length for the RAVI oscillator.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Threshold for bullish or bearish confirmation of the RAVI oscillator (percentage value).
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RaviIaoStrategy"/>.
	/// </summary>
	public RaviIaoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame for analysis", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period inside RAVI", "RAVI");

		_slowLength = Param(nameof(SlowLength), 72)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period inside RAVI", "RAVI");

		_threshold = Param(nameof(Threshold), 0.3m)
			.SetDisplay("RAVI Threshold", "Minimum absolute RAVI value to confirm the trend", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price units", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price units", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRavi = null;
		_prevPrevRavi = null;
		_prevAc = null;
		_prevPrevAc = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		var ao = new AwesomeOscillator();
		_aoAverage = new SMA { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { fastMa, slowMa, ao }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}

		// Use StartProtection for SL/TP
		var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastVal = values[0];
		var slowVal = values[1];
		var aoVal = values[2];

		if (fastVal.IsEmpty || slowVal.IsEmpty || aoVal.IsEmpty)
			return;

		var fastValue = fastVal.ToDecimal();
		var slowValue = slowVal.ToDecimal();
		var aoValue = aoVal.ToDecimal();

		// Compute AC = AO - SMA(AO)
		var aoAvgResult = _aoAverage.Process(aoVal);
		if (aoAvgResult.IsEmpty)
			return;

		var aoAvgValue = aoAvgResult.ToDecimal();
		var ac = aoValue - aoAvgValue;

		if (slowValue == 0m)
		{
			UpdateHistory(null, ac);
			return;
		}

		var ravi = 100m * (fastValue - slowValue) / slowValue;

		if (_prevRavi is decimal prevRavi &&
			_prevPrevRavi is decimal prevPrevRavi &&
			_prevAc is decimal prevAc &&
			_prevPrevAc is decimal prevPrevAc &&
			Position == 0 &&
			IsFormedAndOnlineAndAllowTrading())
		{
			var bullish = prevAc > prevPrevAc && prevPrevAc > 0m && prevRavi > prevPrevRavi && prevRavi > Threshold;
			var bearish = prevAc < prevPrevAc && prevPrevAc < 0m && prevRavi < prevPrevRavi && prevRavi < -Threshold;

			if (bullish)
			{
				BuyMarket(Volume);
			}
			else if (bearish)
			{
				SellMarket(Volume);
			}
		}

		UpdateHistory(ravi, ac);
	}

	private void UpdateHistory(decimal? ravi, decimal ac)
	{
		_prevPrevRavi = _prevRavi;
		_prevRavi = ravi;
		_prevPrevAc = _prevAc;
		_prevAc = ac;
	}
}

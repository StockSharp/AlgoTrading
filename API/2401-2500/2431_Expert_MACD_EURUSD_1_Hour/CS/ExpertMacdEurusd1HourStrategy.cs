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
/// MACD based strategy converted from MetaTrader 5 Expert Advisor.
/// Uses MACD indicator with custom pattern checks on recent values and optional trailing stop.
/// </summary>
public class ExpertMacdEurusd1HourStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<DataType> _candleType;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _signalEma;

	private decimal _main0, _main1;
	private decimal _signal0, _signal1;
	private int _counter;

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingPoints { get => _trailingPoints.Value; set => _trailingPoints.Value = value; }

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy with default parameters.
	/// </summary>
	public ExpertMacdEurusd1HourStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast EMA length for MACD", "Parameters")
			;

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow EMA length for MACD", "Parameters")
			;

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "Signal length for MACD", "Parameters")
			;

		_trailingPoints = Param(nameof(TrailingPoints), 25m)
			.SetDisplay("Trailing Points", "Trailing stop distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_main0 = _main1 = 0m;
		_signal0 = _signal1 = 0m;
		_counter = 0;
		_fastEma = null;
		_slowEma = null;
		_signalEma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		_signalEma = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _fastEma.Process(candle.ClosePrice, candle.CloseTime, true).ToDecimal();
		var slow = _slowEma.Process(candle.ClosePrice, candle.CloseTime, true).ToDecimal();
		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		var main = fast - slow;
		var signal = _signalEma.Process(main, candle.CloseTime, true).ToDecimal();
		if (!_signalEma.IsFormed)
			return;

		// shift stored values
		_main1 = _main0;
		_main0 = main;

		_signal1 = _signal0;
		_signal0 = signal;

		if (_counter < 3)
		{
			_counter++;
			return;
		}

		var buySignal = _main1 <= _signal1 && _main0 > _signal0 && _main0 < 0m;
		var sellSignal = _main1 >= _signal1 && _main0 < _signal0 && _main0 > 0m;

		if (buySignal && Position <= 0)
			BuyMarket();
		else if (sellSignal && Position >= 0)
			SellMarket();
	}
}

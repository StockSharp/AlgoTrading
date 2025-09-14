using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JMA slope based strategy detecting zero breakouts or slope twists.
/// </summary>
public class JmaSlopeStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<JmaSlopeMode> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevJma;
	private decimal? _prevSlope1;
	private decimal? _prevSlope2;
	private decimal? _prevSlope3;

	/// <summary>
	/// JMA smoothing length.
	/// </summary>
	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }

	/// <summary>
	/// JMA phase parameter from -100 to 100.
	/// </summary>
	public int JmaPhase { get => _jmaPhase.Value; set => _jmaPhase.Value = value; }

	/// <summary>
	/// Entry mode algorithm.
	/// </summary>
	public JmaSlopeMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="JmaSlopeStrategy"/>.
	/// </summary>
	public JmaSlopeStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik Moving Average", "Indicators")
			.SetCanOptimize(true);

		_jmaPhase = Param(nameof(JmaPhase), 0)
			.SetDisplay("JMA Phase", "Phase parameter", "Indicators");

		_mode = Param(nameof(Mode), JmaSlopeMode.Breakdown)
			.SetDisplay("Mode", "Entry algorithm", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevJma = null;
		_prevSlope1 = null;
		_prevSlope2 = null;
		_prevSlope3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage { Length = JmaLength, Phase = JmaPhase };

		SubscribeCandles(CandleType)
			.Bind(jma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.StepPrice ?? 1m;
		var slope = _prevJma is decimal prev ? (jmaValue - prev) / step : null;

		if (_prevSlope2 is decimal s2 && _prevSlope1 is decimal s1 && _prevSlope3 is decimal s3)
		{
			var buy = false;
			var sell = false;

			switch (Mode)
			{
				case JmaSlopeMode.Breakdown:
					buy = s2 > 0m && s1 <= 0m;
					sell = s2 < 0m && s1 >= 0m;
					break;

				case JmaSlopeMode.Twist:
					buy = s2 < s3 && s1 > s2;
					sell = s2 > s3 && s1 < s2;
					break;
			}

			if (buy && Position <= 0)
				BuyMarket(Position < 0 ? -Position + Volume : Volume);
			else if (sell && Position >= 0)
				SellMarket(Position > 0 ? Position + Volume : Volume);
		}

		_prevSlope3 = _prevSlope2;
		_prevSlope2 = _prevSlope1;
		_prevSlope1 = slope;
		_prevJma = jmaValue;
	}
}

/// <summary>
/// JMA slope entry modes.
/// </summary>
public enum JmaSlopeMode
{
	/// <summary>
	/// Signals when slope crosses zero.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Signals when slope changes direction.
	/// </summary>
	Twist
}

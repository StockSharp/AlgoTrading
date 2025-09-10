using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that calculates the 3D Wave-PM values for multiple periods.
/// </summary>
public class ThreeDWavePmStrategy : Strategy
{
	private readonly StrategyParam<int> _startPeriod;
	private readonly StrategyParam<int> _periodOffset;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation[] _deviations = Array.Empty<StandardDeviation>();
	private SMA[] _smas = Array.Empty<SMA>();
	private decimal[] _values = Array.Empty<decimal>();

	/// <summary>
	/// Starting period for the first Wave-PM calculation.
	/// </summary>
	public int StartPeriod
	{
		get => _startPeriod.Value;
		set => _startPeriod.Value = value;
	}

	/// <summary>
	/// Offset between consecutive periods.
	/// </summary>
	public int PeriodOffset
	{
		get => _periodOffset.Value;
		set => _periodOffset.Value = value;
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
	/// Initializes a new instance of the <see cref="ThreeDWavePmStrategy"/> class.
	/// </summary>
	public ThreeDWavePmStrategy()
	{
		_startPeriod = Param(nameof(StartPeriod), 20)
			.SetDisplay("Starting Period", "Initial period for Wave-PM", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_periodOffset = Param(nameof(PeriodOffset), 20)
			.SetDisplay("Period Offset", "Offset between consecutive periods", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

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

		_deviations = Array.Empty<StandardDeviation>();
		_smas = Array.Empty<SMA>();
		_values = Array.Empty<decimal>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_deviations = new StandardDeviation[30];
		_smas = new SMA[30];
		_values = new decimal[30];

		for (var i = 0; i < 30; i++)
		{
			var length = StartPeriod + (i * PeriodOffset);
			_deviations[i] = new StandardDeviation { Length = length };
			_smas[i] = new SMA { Length = 100 };
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = 0; i < _deviations.Length; i++)
		{
			var devValue = _deviations[i].Process(candle).ToDecimal();
			var devSquare = devValue * devValue;
			var smaValue = _smas[i].Process(devSquare, candle.ServerTime, true).ToDecimal();
			var temp = (decimal)Math.Sqrt((double)smaValue);
			temp = temp != 0 ? devValue / temp : 0m;

			decimal result;
			if (temp > 0)
			{
				var iexp = (decimal)Math.Exp((double)(-2m * temp));
				result = (1 - iexp) / (1 + iexp);
			}
			else
			{
				var iexp = (decimal)Math.Exp((double)(2m * temp));
				result = (iexp - 1) / (1 + iexp);
			}

			_values[i] = result;
		}
	}
}


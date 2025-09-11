namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// SMU STDEV Candles Strategy.
/// Uses standard deviation of candle prices and trades on its change.
/// </summary>
public class SmuStdevCandlesStrategy : Strategy
{
	private StandardDeviation _openStd = null!;
	private StandardDeviation _highStd = null!;
	private StandardDeviation _lowStd = null!;
	private StandardDeviation _closeStd = null!;
	private decimal? _prevCloseStd;

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _scale;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// STDEV calculation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Scale factor for STDEV values.
	/// </summary>
	public decimal Scale
	{
		get => _scale.Value;
		set => _scale.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SmuStdevCandlesStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("STDEV Length", "Calculation length", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_scale = Param(nameof(Scale), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Scale", "Value multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

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

		_openStd = new StandardDeviation { Length = Length };
		_highStd = new StandardDeviation { Length = Length };
		_lowStd = new StandardDeviation { Length = Length };
		_closeStd = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);

		subscription
				.Bind(ProcessCandle)
				.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
				return;

		_ = _openStd.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal() * Scale;
		_ = _highStd.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal() * Scale;
		_ = _lowStd.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal() * Scale;
		var closeStd = _closeStd.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal() * Scale;

		if (_prevCloseStd is null)
		{
			_prevCloseStd = closeStd;
			return;
		}

		if (closeStd > _prevCloseStd && Position <= 0)
				BuyMarket();
		if (closeStd < _prevCloseStd && Position >= 0)
				SellMarket();

		_prevCloseStd = closeStd;
	}
}


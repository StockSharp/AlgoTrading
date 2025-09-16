using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the slope change of a double-smoothed moving average.
/// Uses an EMA and JMA combination to detect trend reversals.
/// </summary>
public class ColorXvaMADigitStrategy : Strategy
{
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _slowMa;
	private JurikMovingAverage _fastMa;

	private int _previousDirection;

	/// <summary>
	/// Length of the slow EMA.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Length of the fast JMA.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ColorXvaMADigitStrategy()
	{
		_slowLength = Param(nameof(SlowLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "JMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_slowMa = null;
		_fastMa = null;
		_previousDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_slowMa = new ExponentialMovingAverage { Length = SlowLength };
		_fastMa = new JurikMovingAverage { Length = FastLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowMa, _fastMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastMa);
			DrawOwnTrades(area);
		}

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_slowMa.IsFormed || !_fastMa.IsFormed)
			return;

		var direction = fastValue > slowValue ? 1 : -1;
		if (_previousDirection == 0)
		{
			_previousDirection = direction;
			return;
		}

		if (direction != _previousDirection)
		{
			var volume = Volume + Math.Abs(Position);
			if (direction > 0 && Position <= 0)
				BuyMarket(volume);
			else if (direction < 0 && Position >= 0)
				SellMarket(volume);
		}

		_previousDirection = direction;
	}
}

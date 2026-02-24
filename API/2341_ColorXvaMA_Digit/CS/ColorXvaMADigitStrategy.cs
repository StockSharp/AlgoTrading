using System;
using System.Collections.Generic;

using Ecng.Common;

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

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorXvaMADigitStrategy()
	{
		_slowLength = Param(nameof(SlowLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "EMA period", "Indicators");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "JMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousDirection = 0;
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
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousDirection = fastValue > slowValue ? 1 : -1;
			return;
		}

		var direction = fastValue > slowValue ? 1 : -1;

		if (direction != _previousDirection && _previousDirection != 0)
		{
			if (direction > 0 && Position <= 0)
				BuyMarket();
			else if (direction < 0 && Position >= 0)
				SellMarket();
		}

		_previousDirection = direction;
	}
}

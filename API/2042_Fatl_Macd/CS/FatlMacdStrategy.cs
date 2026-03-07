using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FATL MACD trend-following strategy.
/// Opens long positions when the indicator turns upward
/// and short positions when it turns downward.
/// </summary>
public class FatlMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private bool _isInitialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FatlMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast EMA", "Period of the fast moving average", "MACD")
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow EMA", "Period of the slow moving average", "MACD")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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
		_prev1 = default;
		_prev2 = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastLength },
			LongMa = { Length = SlowLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prev2 = _prev1 = macdValue;
			_isInitialized = true;
			return;
		}

		// Indicator turned upward
		if (_prev1 < _prev2)
		{
			if (Position < 0)
				BuyMarket();

			if (macdValue > _prev1 && Position <= 0)
				BuyMarket();
		}
		// Indicator turned downward
		else if (_prev1 > _prev2)
		{
			if (Position > 0)
				SellMarket();

			if (macdValue < _prev1 && Position >= 0)
				SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = macdValue;
	}
}

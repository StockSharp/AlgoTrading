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
/// Two moving average crossover strategy converted from "2MA Bunny Cross Expert".
/// Uses fast and slow simple moving averages to generate signals.
/// </summary>
public class TwoMaBunnyCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFormed;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TwoMaBunnyCrossStrategy"/>.
	/// </summary>
	public TwoMaBunnyCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of fast moving average", "Parameters")
			;

		_slowLength = Param(nameof(SlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of slow moving average", "Parameters")
			;
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_isFormed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastSma = new SMA { Length = FastLength };
		_slowSma = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast != 0m && _prevSlow != 0m)
		{
			var buySignal = _prevFast <= _prevSlow && fast > slow;
			var sellSignal = _prevFast >= _prevSlow && fast < slow;

			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
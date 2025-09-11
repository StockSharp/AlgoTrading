using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// QQQ main MA crossover strategy with trend filters.
/// Enters long when price crosses above main MA and the MA is rising.
/// Enters short when price crosses below main MA and the MA is falling.
/// </summary>
public class QqqStrategyV2EslEasyPeasyXStrategy : Strategy
{
	private readonly StrategyParam<int> _mainMaLength;
	private readonly StrategyParam<int> _trendLongLength;
	private readonly StrategyParam<int> _trendShortLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMainMa;
	private decimal _prevClose;
	private bool _isFirst = true;

	public int MainMaLength
	{
		get => _mainMaLength.Value;
		set => _mainMaLength.Value = value;
	}

	public int TrendLongLength
	{
		get => _trendLongLength.Value;
		set => _trendLongLength.Value = value;
	}

	public int TrendShortLength
	{
		get => _trendShortLength.Value;
		set => _trendShortLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public QqqStrategyV2EslEasyPeasyXStrategy()
	{
		_mainMaLength = Param(nameof(MainMaLength), 200)
			.SetRange(50, 400)
			.SetDisplay("Main MA Length", "Length of main moving average", "MA Settings")
			.SetCanOptimize();

		_trendLongLength = Param(nameof(TrendLongLength), 100)
			.SetRange(20, 200)
			.SetDisplay("Trend Long Length", "Trend filter length for long trades", "MA Settings")
			.SetCanOptimize();

		_trendShortLength = Param(nameof(TrendShortLength), 50)
			.SetRange(20, 200)
			.SetDisplay("Trend Short Length", "Trend filter length for short trades", "MA Settings")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mainMa = new ExponentialMovingAverage { Length = MainMaLength };
		var trendLongMa = new ExponentialMovingAverage { Length = TrendLongLength };
		var trendShortMa = new ExponentialMovingAverage { Length = TrendShortLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mainMa, trendLongMa, trendShortMa, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mainMa, decimal trendLong, decimal trendShort)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevMainMa = mainMa;
			_prevClose = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		var crossedAbove = _prevClose <= _prevMainMa && candle.ClosePrice > mainMa;
		var crossedBelow = _prevClose >= _prevMainMa && candle.ClosePrice < mainMa;

		var slopeUp = mainMa > _prevMainMa;
		var slopeDown = mainMa < _prevMainMa;

		if (crossedAbove && slopeUp && candle.ClosePrice > trendLong && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossedBelow && slopeDown && candle.ClosePrice < trendShort && Position >= 0)
		{
			SellMarket();
		}

		_prevMainMa = mainMa;
		_prevClose = candle.ClosePrice;
	}
}


using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on SMA over custom timeframe.
/// </summary>
public class MtfSecondsValuesJDStrategy : Strategy
{
	private readonly StrategyParam<int> _averageLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private bool _prevAbove;
	private bool _hasPrev;
	private int _barIndex;
	private int _lastTradeBar = -1000000;

	public int AverageLength { get => _averageLength.Value; set => _averageLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MtfSecondsValuesJDStrategy()
	{
		_averageLength = Param(nameof(AverageLength), 20).SetGreaterThanZero();
		_cooldownBars = Param(nameof(CooldownBars), 4).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		_prevAbove = false;
		_barIndex = 0;
		_lastTradeBar = -1000000;

		var sma = new SimpleMovingAverage { Length = AverageLength };
		var dummyEma = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, dummyEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var above = candle.ClosePrice > smaValue;
		if (!_hasPrev)
		{
			_prevAbove = above;
			_hasPrev = true;
			return;
		}

		var canTrade = _barIndex - _lastTradeBar >= CooldownBars;
		var crossUp = !_prevAbove && above;
		var crossDown = _prevAbove && !above;

		if (canTrade && crossUp && Position <= 0)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (canTrade && crossDown && Position >= 0)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevAbove = above;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevAbove = false;
		_hasPrev = false;
		_barIndex = 0;
		_lastTradeBar = -1000000;
	}
}

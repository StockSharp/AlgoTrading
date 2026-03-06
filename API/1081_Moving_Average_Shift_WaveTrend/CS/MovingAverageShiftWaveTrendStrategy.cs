using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Shift WaveTrend Strategy.
/// Uses EMA crossover with momentum oscillator.
/// </summary>
public class MovingAverageShiftWaveTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _barIndex;
	private int _lastTradeBar = int.MinValue;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal MinSpreadPercent { get => _minSpreadPercent.Value; set => _minSpreadPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageShiftWaveTrendStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 21).SetGreaterThanZero();
		_cooldownBars = Param(nameof(CooldownBars), 6).SetGreaterThanZero();
		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.01m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_barIndex = 0;
		_lastTradeBar = int.MinValue;

		var fast = new EMA { Length = FastLength };
		var slow = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		var spreadPercent = candle.ClosePrice != 0m
			? Math.Abs(fast - slow) / candle.ClosePrice * 100m
			: 0m;
		var canTrade = _barIndex - _lastTradeBar >= CooldownBars;
		var crossUp = _prevFast <= _prevSlow && fast > slow && spreadPercent >= MinSpreadPercent;
		var crossDown = _prevFast >= _prevSlow && fast < slow && spreadPercent >= MinSpreadPercent;

		if (canTrade && crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastTradeBar = _barIndex;
		}
		else if (canTrade && crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastTradeBar = _barIndex;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastTradeBar = int.MinValue;
	}
}

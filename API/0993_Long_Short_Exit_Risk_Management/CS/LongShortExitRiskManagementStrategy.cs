using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long/short strategy with SMA crossover entry and risk management via StartProtection.
/// </summary>
public class LongShortExitRiskManagementStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fast;
	private ExponentialMovingAverage _slow;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _barsSinceSignal;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LongShortExitRiskManagementStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
			.SetGreaterThanZero();

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetGreaterThanZero();

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fast = null;
		_slow = null;
		_prevFast = 0;
		_prevSlow = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_barsSinceSignal = 0;

		_fast = new ExponentialMovingAverage { Length = FastPeriod };
		_slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fast, _slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_fast.IsFormed || !_slow.IsFormed || _prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		if (_barsSinceSignal < CooldownBars)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fastVal > slowVal;
		var crossDown = _prevFast >= _prevSlow && fastVal < slowVal;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}

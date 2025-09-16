using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two smoothed moving average crossover strategy with level offsets.
/// </summary>
public class TwoMaFourLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _mostTopLevel;
	private readonly StrategyParam<int> _topLevel;
	private readonly StrategyParam<int> _lowerLevel;
	private readonly StrategyParam<int> _lowermostLevel;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _fastMa = null!;
	private SmoothedMovingAverage _slowMa = null!;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int MostTopLevel { get => _mostTopLevel.Value; set => _mostTopLevel.Value = value; }
	public int TopLevel { get => _topLevel.Value; set => _topLevel.Value = value; }
	public int LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }
	public int LowermostLevel { get => _lowermostLevel.Value; set => _lowermostLevel.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TwoMaFourLevelStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Period of the fast smoothed MA", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(20, 150, 5);

		_slowPeriod = Param(nameof(SlowPeriod), 130)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Period of the slow smoothed MA", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(60, 300, 5);

		_mostTopLevel = Param(nameof(MostTopLevel), 500)
			.SetGreaterThanZero()
			.SetDisplay("Extreme Upper Level", "Highest positive offset in points", "Levels");

		_topLevel = Param(nameof(TopLevel), 250)
			.SetGreaterThanZero()
			.SetDisplay("Upper Level", "Second positive offset in points", "Levels");

		_lowerLevel = Param(nameof(LowerLevel), 250)
			.SetGreaterThanZero()
			.SetDisplay("Lower Level", "Second negative offset in points", "Levels");

		_lowermostLevel = Param(nameof(LowermostLevel), 500)
			.SetGreaterThanZero()
			.SetDisplay("Extreme Lower Level", "Largest negative offset in points", "Levels");

		_takeProfitPips = Param(nameof(TakeProfitPips), 55)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to take profit", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 260)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (FastPeriod >= SlowPeriod)
		{
			this.AddErrorLog("FastPeriod must be less than SlowPeriod.");
			Stop();
			return;
		}

		if (MostTopLevel <= TopLevel)
		{
			this.AddErrorLog("MostTopLevel must be greater than TopLevel.");
			Stop();
			return;
		}

		if (LowerLevel >= LowermostLevel)
		{
			this.AddErrorLog("LowerLevel must be less than LowermostLevel.");
			Stop();
			return;
		}

		_fastMa = new SmoothedMovingAverage { Length = FastPeriod };
		_slowMa = new SmoothedMovingAverage { Length = SlowPeriod };

		var pip = Security?.PriceStep ?? 1m;

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * pip, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * pip, UnitTypes.Absolute));

		var subscription = SubscribeCandles(CandleType);
		subscription.ForEach(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, median, candle.OpenTime));
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, median, candle.OpenTime));

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var pip = Security?.PriceStep ?? 1m;
		var signal = GetSignal(fast, slow, _prevFast.Value, _prevSlow.Value, pip);

		if (Position == 0)
		{
			if (signal > 0)
			{
				// Enter long position after bullish crossover confirmation.
				BuyMarket();
			}
			else if (signal < 0)
			{
				// Enter short position after bearish crossover confirmation.
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private int GetSignal(decimal fast, decimal slow, decimal prevFast, decimal prevSlow, decimal pip)
	{
		if (IsCrossUp(prevFast, fast, prevSlow, slow, 0m) ||
			IsCrossUp(prevFast, fast, prevSlow, slow, MostTopLevel * pip) ||
			IsCrossUp(prevFast, fast, prevSlow, slow, TopLevel * pip) ||
			IsCrossUp(prevFast, fast, prevSlow, slow, -LowermostLevel * pip) ||
			IsCrossUp(prevFast, fast, prevSlow, slow, -LowerLevel * pip))
		{
			return 1;
		}

		if (IsCrossDown(prevFast, fast, prevSlow, slow, 0m) ||
			IsCrossDown(prevFast, fast, prevSlow, slow, MostTopLevel * pip) ||
			IsCrossDown(prevFast, fast, prevSlow, slow, TopLevel * pip) ||
			IsCrossDown(prevFast, fast, prevSlow, slow, -LowermostLevel * pip) ||
			IsCrossDown(prevFast, fast, prevSlow, slow, -LowerLevel * pip))
		{
			return -1;
		}

		return 0;
	}

	private static bool IsCrossUp(decimal prevFast, decimal fast, decimal prevSlow, decimal slow, decimal offset)
	{
		var prevSlowShifted = prevSlow + offset;
		var slowShifted = slow + offset;
		return prevFast <= prevSlowShifted && fast > slowShifted;
	}

	private static bool IsCrossDown(decimal prevFast, decimal fast, decimal prevSlow, decimal slow, decimal offset)
	{
		var prevSlowShifted = prevSlow + offset;
		var slowShifted = slow + offset;
		return prevFast >= prevSlowShifted && fast < slowShifted;
	}
}

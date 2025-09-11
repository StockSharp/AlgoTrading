using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on 2-year yield spread momentum with Bollinger Bands.
/// </summary>
public class ForexPairYieldMomentumStrategy : Strategy
{
	private readonly StrategyParam<Security> _yieldASecurity;
	private readonly StrategyParam<Security> _yieldBSecurity;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerStdDev;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<bool> _reverseLogic;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _spreadSma;
	private SimpleMovingAverage _momentumSma;
	private StandardDeviation _momentumStd;
	private decimal? _yieldA;
	private decimal? _yieldB;
	private int _barsInPosition;

	public Security YieldASecurity
	{
		get => _yieldASecurity.Value;
		set => _yieldASecurity.Value = value;
	}

	public Security YieldBSecurity
	{
		get => _yieldBSecurity.Value;
		set => _yieldBSecurity.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	public decimal BollingerStdDev
	{
		get => _bollingerStdDev.Value;
		set => _bollingerStdDev.Value = value;
	}

	public int HoldPeriods
	{
		get => _holdPeriods.Value;
		set => _holdPeriods.Value = value;
	}

	public bool ReverseLogic
	{
		get => _reverseLogic.Value;
		set => _reverseLogic.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ForexPairYieldMomentumStrategy()
	{
		_yieldASecurity = Param<Security>(nameof(YieldASecurity), null)
			.SetDisplay("Yield Security A", "First yield security", "General");

		_yieldBSecurity = Param<Security>(nameof(YieldBSecurity), null)
			.SetDisplay("Yield Security B", "Second yield security", "General");

		_momentumLength = Param(nameof(MomentumLength), 26)
			.SetDisplay("Momentum Length", "Period for yield spread average", "Parameters")
			.SetCanOptimize(true);

		_bollingerLength = Param(nameof(BollingerLength), 24)
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands", "Parameters")
			.SetCanOptimize(true);

		_bollingerStdDev = Param(nameof(BollingerStdDev), 1m)
			.SetDisplay("Bollinger Std Dev", "StdDev multiplier for bands", "Parameters")
			.SetCanOptimize(true);

		_holdPeriods = Param(nameof(HoldPeriods), 20)
			.SetDisplay("Hold Periods", "Bars to hold a position", "Parameters")
			.SetCanOptimize(true);

		_reverseLogic = Param(nameof(ReverseLogic), false)
			.SetDisplay("Reverse Logic", "Invert long/short conditions", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_spreadSma = new SimpleMovingAverage { Length = MomentumLength };
		_momentumSma = new SimpleMovingAverage { Length = BollingerLength };
		_momentumStd = new StandardDeviation { Length = BollingerLength };

		if (YieldASecurity != null)
		{
			var subA = SubscribeCandles(CandleType, true, YieldASecurity);
			subA.Bind(ProcessYieldA).Start();
		}

		if (YieldBSecurity != null)
		{
			var subB = SubscribeCandles(CandleType, true, YieldBSecurity);
			subB.Bind(ProcessYieldB).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessMain).Start();

		StartProtection();
	}

	private void ProcessYieldA(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_yieldA = candle.ClosePrice;
	}

	private void ProcessYieldB(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_yieldB = candle.ClosePrice;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_yieldA is not decimal a || _yieldB is not decimal b)
			return;

		var spread = a - b;
		var spreadValue = _spreadSma.Process(spread, candle.ServerTime, true);

		if (!spreadValue.IsFinal || spreadValue.GetValue<decimal>() is not decimal spreadAvg)
			return;

		var momentum = spread - spreadAvg;

		var middleVal = _momentumSma.Process(momentum, candle.ServerTime, true);
		if (!middleVal.IsFinal || middleVal.GetValue<decimal>() is not decimal middle)
			return;

		var stdVal = (StandardDeviationValue)_momentumStd.Process(momentum, candle.ServerTime, true);
		if (!stdVal.IsFinal || stdVal.IndicatorValue is not decimal std)
			return;

		var upper = middle + BollingerStdDev * std;
		var lower = middle - BollingerStdDev * std;

		var longCond = ReverseLogic ? momentum > upper : momentum < lower;
		var shortCond = ReverseLogic ? momentum < lower : momentum > upper;

		if (longCond && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			_barsInPosition = 0;
			return;
		}

		if (shortCond && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
			_barsInPosition = 0;
			return;
		}

		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldPeriods)
			{
				CancelActiveOrders();

				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);

				_barsInPosition = 0;
			}
		}
	}
}

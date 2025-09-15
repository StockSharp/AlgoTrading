namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the AFL Winner indicator approximation using a stochastic oscillator.
/// </summary>
public class AflWinnerV2Strategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;

	private decimal? _prevColor;

	public AflWinnerV2Strategy()
	{
		_kPeriod = Param<int>("KPeriod", 5).SetDisplay("%K Period").SetCanOptimize(true);
		_dPeriod = Param<int>("DPeriod", 3).SetDisplay("%D Period").SetCanOptimize(true);
		_highLevel = Param<decimal>("HighLevel", 40m).SetDisplay("High Level").SetCanOptimize(true);
		_lowLevel = Param<decimal>("LowLevel", -40m).SetDisplay("Low Level").SetCanOptimize(true);
	}

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished || !stochValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.Main is not decimal k || stoch.Signal is not decimal d)
			return;

		int color;

		if (k > d)
			color = (k > HighLevel || (k > LowLevel && d <= LowLevel)) ? 3 : 2;
		else
			color = (k < LowLevel || (d > HighLevel && k <= HighLevel)) ? 0 : 1;

		if (color == 3 && _prevColor != 3)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (color == 0 && _prevColor != 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (color < 2 && Position > 0)
		{
			SellMarket(Position);
		}
		else if (color > 1 && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevColor = color;
	}
}

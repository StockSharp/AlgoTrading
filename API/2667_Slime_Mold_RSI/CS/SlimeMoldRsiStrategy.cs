using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Slime Mold RSI perceptron strategy converted from MQL4.
/// The strategy sums weighted RSI inputs to generate zero-crossing signals.
/// </summary>
public class SlimeMoldRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _weight1;
	private readonly StrategyParam<decimal> _weight2;
	private readonly StrategyParam<decimal> _weight3;
	private readonly StrategyParam<decimal> _weight4;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex? _rsi12;
	private RelativeStrengthIndex? _rsi36;
	private RelativeStrengthIndex? _rsi108;
	private RelativeStrengthIndex? _rsi324;

	private decimal? _previousPerceptron;

	/// <summary>
	/// Weight applied to the 12-period RSI input.
	/// </summary>
	public decimal Weight1
	{
		get => _weight1.Value;
		set => _weight1.Value = value;
	}

	/// <summary>
	/// Weight applied to the 36-period RSI input.
	/// </summary>
	public decimal Weight2
	{
		get => _weight2.Value;
		set => _weight2.Value = value;
	}

	/// <summary>
	/// Weight applied to the 108-period RSI input.
	/// </summary>
	public decimal Weight3
	{
		get => _weight3.Value;
		set => _weight3.Value = value;
	}

	/// <summary>
	/// Weight applied to the 324-period RSI input.
	/// </summary>
	public decimal Weight4
	{
		get => _weight4.Value;
		set => _weight4.Value = value;
	}

	/// <summary>
	/// Candle type used for RSI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SlimeMoldRsiStrategy"/> class.
	/// </summary>
	public SlimeMoldRsiStrategy()
	{
		_weight1 = Param(nameof(Weight1), -100m)
			.SetDisplay("Weight 1", "Weight applied to the 12-period RSI input", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(-200m, 200m, 10m);

		_weight2 = Param(nameof(Weight2), -100m)
			.SetDisplay("Weight 2", "Weight applied to the 36-period RSI input", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(-200m, 200m, 10m);

		_weight3 = Param(nameof(Weight3), -100m)
			.SetDisplay("Weight 3", "Weight applied to the 108-period RSI input", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(-200m, 200m, 10m);

		_weight4 = Param(nameof(Weight4), -100m)
			.SetDisplay("Weight 4", "Weight applied to the 324-period RSI input", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(-200m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles used in calculations", "General");
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

		// Drop cached indicator instances and perceptron history.
		_rsi12 = null;
		_rsi36 = null;
		_rsi108 = null;
		_rsi324 = null;
		_previousPerceptron = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create RSI indicators for each horizon used by the original perceptron.
		_rsi12 = new RelativeStrengthIndex { Length = 12 };
		_rsi36 = new RelativeStrengthIndex { Length = 36 };
		_rsi108 = new RelativeStrengthIndex { Length = 108 };
		_rsi324 = new RelativeStrengthIndex { Length = 324 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_rsi12 is null || _rsi36 is null || _rsi108 is null || _rsi324 is null)
			return;

		// Median price replicates PRICE_MEDIAN used in the original script.
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		var rsi12Value = _rsi12.Process(medianPrice, candle.ServerTime, true).ToDecimal();
		var rsi36Value = _rsi36.Process(medianPrice, candle.ServerTime, true).ToDecimal();
		var rsi108Value = _rsi108.Process(medianPrice, candle.ServerTime, true).ToDecimal();
		var rsi324Value = _rsi324.Process(medianPrice, candle.ServerTime, true).ToDecimal();

		// Wait until every RSI is fully formed before evaluating signals.
		if (!_rsi12.IsFormed || !_rsi36.IsFormed || !_rsi108.IsFormed || !_rsi324.IsFormed)
			return;

		var currentPerceptron =
			(Weight1 * NormalizeRsi(rsi12Value)) +
			(Weight2 * NormalizeRsi(rsi36Value)) +
			(Weight3 * NormalizeRsi(rsi108Value)) +
			(Weight4 * NormalizeRsi(rsi324Value));

		// Initialize the history with the first complete value.
		if (_previousPerceptron is null)
		{
			_previousPerceptron = currentPerceptron;
			return;
		}

		var previousPerceptron = _previousPerceptron.Value;

		// Even if trading is disabled, keep the state in sync with the incoming data.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousPerceptron = currentPerceptron;
			return;
		}

		// Zero-crossing from negative to positive triggers a long entry.
		if (previousPerceptron < 0m && currentPerceptron > 0m && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Long entry. Previous perceptron: {previousPerceptron:F2}, current: {currentPerceptron:F2}");
			}
		}
		// Zero-crossing from positive to negative triggers a short entry.
		else if (previousPerceptron > 0m && currentPerceptron < 0m && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Short entry. Previous perceptron: {previousPerceptron:F2}, current: {currentPerceptron:F2}");
			}
		}

		// Store the latest perceptron value for the next signal evaluation.
		_previousPerceptron = currentPerceptron;
	}

	private static decimal NormalizeRsi(decimal rsiValue)
	{
		// Transform RSI from [0,100] into [-1,+1] as in the original script.
		return ((rsiValue / 100m) - 0.5m) * 2m;
	}
}

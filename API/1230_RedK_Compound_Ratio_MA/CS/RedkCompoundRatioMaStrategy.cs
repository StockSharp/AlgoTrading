using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RedkCompoundRatioMaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _ratioMultiplier;
	private readonly StrategyParam<bool> _autoSmoothing;
	private readonly StrategyParam<int> _manualSmoothing;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private WeightedMovingAverage _coraWma;
	private decimal? _prevCoraWave;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal RatioMultiplier { get => _ratioMultiplier.Value; set => _ratioMultiplier.Value = value; }
	public bool AutoSmoothing { get => _autoSmoothing.Value; set => _autoSmoothing.Value = value; }
	public int ManualSmoothing { get => _manualSmoothing.Value; set => _manualSmoothing.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RedkCompoundRatioMaStrategy()
	{
		_length = Param(nameof(Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Period for CoRa Wave", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_ratioMultiplier = Param(nameof(RatioMultiplier), 2m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Comp Ratio Mult", "Multiplier for compound ratio", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(0m, 5m, 0.1m);

		_autoSmoothing = Param(nameof(AutoSmoothing), true)
		.SetDisplay("Auto Smoothing", "Use auto smoothing", "Smoothing");

		_manualSmoothing = Param(nameof(ManualSmoothing), 1)
		.SetGreaterThanZero()
		.SetDisplay("Manual Smoothing", "Manual smoothing length", "Smoothing")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prices.Clear();
		_prevCoraWave = null;
		_coraWma?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var smoothing = AutoSmoothing ? Math.Max((int)Math.Round(Math.Sqrt(Length)), 1) : ManualSmoothing;
		_coraWma = new WeightedMovingAverage { Length = smoothing };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_prices.Add(price);
		if (_prices.Count > Length)
			_prices.RemoveAt(0);

		if (_prices.Count < Length || _coraWma == null)
			return;

		const decimal startWt = 0.01m;
		var endWt = Length;
		var r = (decimal)Math.Pow((double)(endWt / startWt), 1m / (Length - 1)) - 1m;
		var baseVal = 1m + r * RatioMultiplier;

		decimal numerator = 0m;
		decimal denom = 0m;
		for (var i = 0; i < Length; i++)
		{
			var cWeight = startWt * (decimal)Math.Pow((double)baseVal, Length - i);
			numerator += _prices[i] * cWeight;
			denom += cWeight;
		}

		var coraRaw = numerator / denom;
		var coraValue = _coraWma.Process(coraRaw);
		if (!coraValue.IsFinal)
			return;

		var coraWave = coraValue.GetValue<decimal>();

		if (_prevCoraWave is decimal prev)
		{
			if (coraWave > prev && Position <= 0)
				BuyMarket();
			else if (coraWave < prev && Position >= 0)
				SellMarket();
		}

		_prevCoraWave = coraWave;
	}
}
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades using adaptive Fractal RSI indicator.
/// </summary>
public class FractalRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<int> _fractalPeriod;
	private readonly StrategyParam<int> _normalSpeed;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;

	private decimal? _previousValue;

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trade direction mode.
	/// </summary>
	public TrendMode Trend
	{
		get => _trend.Value;
		set => _trend.Value = value;
	}

	/// <summary>
	/// Period used for fractal dimension calculation.
	/// </summary>
	public int FractalPeriod
	{
		get => _fractalPeriod.Value;
		set => _fractalPeriod.Value = value;
	}

	/// <summary>
	/// Base period for RSI before fractal adjustment.
	/// </summary>
	public int NormalSpeed
	{
		get => _normalSpeed.Value;
		set => _normalSpeed.Value = value;
	}

	/// <summary>
	/// Upper level for Fractal RSI.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower level for Fractal RSI.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FractalRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");

		_trend = Param(nameof(Trend), TrendMode.Direct)
			.SetDisplay("Trend Mode", "Trade with trend or against it", "Trading");

		_fractalPeriod = Param(nameof(FractalPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Fractal Period", "Period for fractal dimension", "Indicator");

		_normalSpeed = Param(nameof(NormalSpeed), 30)
			.SetGreaterThanZero()
			.SetDisplay("Normal Speed", "Base period for RSI", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetRange(0m, 100m)
			.SetDisplay("High Level", "Upper threshold for Fractal RSI", "Indicator");

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetRange(0m, 100m)
			.SetDisplay("Low Level", "Lower threshold for Fractal RSI", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new FractalRsi
		{
			Period = FractalPeriod,
			NormalSpeed = NormalSpeed
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(indicator, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, indicator);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Point),
			stopLoss: new Unit(StopLoss, UnitTypes.Point)
		);
	}

	private void Process(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prev = _previousValue;
		_previousValue = value;

		if (prev is null)
			return;

		if (Trend == TrendMode.Direct)
		{
			if (prev > LowLevel && value <= LowLevel && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (prev < HighLevel && value >= HighLevel && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		else
		{
			if (prev > LowLevel && value <= LowLevel && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (prev < HighLevel && value >= HighLevel && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
	}

	/// <summary>
	/// Trading direction mode.
	/// </summary>
	public enum TrendMode
	{
		/// <summary>
		/// Trade in the direction of indicator signals.
		/// </summary>
		Direct,
		/// <summary>
		/// Trade opposite to indicator signals.
		/// </summary>
		Against
	}

	private class FractalRsi : Indicator<decimal>
	{
		public int Period { get; set; } = 30;
		public int NormalSpeed { get; set; } = 30;

		private readonly List<decimal> _prices = new();
		private const double Log2 = 0.6931471805599453; // Math.Log(2)

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();
			_prices.Add(price);

			if (_prices.Count < Period + 1)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 50m, input.Time);
			}

			if (_prices.Count > 500)
				_prices.RemoveAt(0);

			var lastIndex = _prices.Count - 1;
			var startIndex = lastIndex - Period + 1;
			var priceMax = _prices[startIndex];
			var priceMin = _prices[startIndex];
			for (var i = startIndex; i <= lastIndex; i++)
			{
				var p = _prices[i];
				if (p > priceMax)
					priceMax = p;
				if (p < priceMin)
					priceMin = p;
			}

			double length = 0.0;
			double? priorDiff = null;

			if (priceMax - priceMin > 0m)
			{
				for (var k = 0; k < Period; k++)
				{
					var p = (double)((_prices[lastIndex - k] - priceMin) / (priceMax - priceMin));
					if (priorDiff != null)
						length += Math.Sqrt(Math.Pow(p - priorDiff.Value, 2.0) + 1.0 / (Period * Period));
					priorDiff = p;
				}
			}

			double fdi = length > 0.0 ? 1.0 + (Math.Log(length) + Log2) / Math.Log(2.0 * (Period - 1)) : 0.0;
			double hurst = 2.0 - fdi;
			double trailDim = hurst != 0.0 ? 1.0 / hurst : 0.0;
			var speed = (int)Math.Max(1, Math.Round(NormalSpeed * trailDim / 2.0));

			if (_prices.Count <= speed)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 50m, input.Time);
			}

			decimal sumUp = 0m;
			decimal sumDown = 0m;
			for (var i = lastIndex - speed + 1; i <= lastIndex; i++)
			{
				var diff = _prices[i] - _prices[i - 1];
				if (diff > 0)
					sumUp += diff;
				else
					sumDown -= diff;
			}

			var pos = sumUp / speed;
			var neg = sumDown / speed;
			decimal rsi;
			if (neg > 0)
				rsi = 100m - (100m / (1m + pos / neg));
			else
				rsi = pos > 0 ? 100m : 50m;

			IsFormed = true;
			return new DecimalIndicatorValue(this, rsi, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_prices.Clear();
		}
	}
}

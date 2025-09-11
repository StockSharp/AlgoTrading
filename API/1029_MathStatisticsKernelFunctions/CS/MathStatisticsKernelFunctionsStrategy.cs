using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates various statistical kernel functions.
/// Places trades when the selected kernel output crosses 0.5.
/// </summary>
public class MathStatisticsKernelFunctionsStrategy : Strategy
{
	private readonly StrategyParam<string> _kernel;
	private readonly StrategyParam<decimal> _bandwidth;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;

	/// <summary>
	/// Kernel function name.
	/// </summary>
	public string Kernel
	{
		get => _kernel.Value;
		set => _kernel.Value = value;
	}

	/// <summary>
	/// Bandwidth value for kernel calculation.
	/// </summary>
	public decimal Bandwidth
	{
		get => _bandwidth.Value;
		set => _bandwidth.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MathStatisticsKernelFunctionsStrategy()
	{
		_kernel = Param(nameof(Kernel), "uniform")
			.SetDisplay("Kernel", "Kernel function name", "General");

		_bandwidth = Param(nameof(Bandwidth), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bandwidth", "Kernel bandwidth", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var test = -1m + (_barIndex % 100) * 0.02m;
		_barIndex++;

		var value = Select(Kernel, test, Bandwidth);

		if (value > 0.5m && Position <= 0)
			BuyMarket();
		else if (value < 0.5m && Position >= 0)
			SellMarket();
	}

	private static decimal Uniform(decimal distance, decimal bandwidth)
	{
		return Math.Abs(distance) > bandwidth ? 0m : 0.5m;
	}

	private static decimal Triangular(decimal distance, decimal bandwidth)
	{
		return Math.Abs(distance) > bandwidth ? 0m : 1m - Math.Abs(distance / bandwidth);
	}

	private static decimal Epanechnikov(decimal distance, decimal bandwidth)
	{
		if (Math.Abs(distance) > bandwidth)
			return 0m;

		var ratio = distance / bandwidth;
		return 0.25m * (1m - ratio * ratio);
	}

	private static decimal Quartic(decimal distance, decimal bandwidth)
	{
		if (Math.Abs(distance) > bandwidth)
			return 0m;

		var ratio = distance / bandwidth;
		var inner = 1m - ratio * ratio;
		return 0.9375m * inner * inner;
	}

	private static decimal Triweight(decimal distance, decimal bandwidth)
	{
		if (Math.Abs(distance) > bandwidth)
			return 0m;

		var ratio = distance / bandwidth;
		var inner = 1m - ratio * ratio;
		return (35m / 32m) * inner * inner * inner;
	}

	private static decimal Tricubic(decimal distance, decimal bandwidth)
	{
		if (Math.Abs(distance) > bandwidth)
			return 0m;

		var ratio = Math.Abs(distance) / bandwidth;
		var inner = 1m - ratio * ratio * ratio;
		return (70m / 81m) * inner * inner * inner;
	}

	private static decimal Gaussian(decimal distance, decimal bandwidth)
	{
		var d = (double)(distance / bandwidth);
		var result = 1d / Math.Sqrt(2d * Math.PI) * Math.Exp(-0.5d * d * d);
		return (decimal)result;
	}

	private static decimal Cosine(decimal distance, decimal bandwidth)
	{
		if (Math.Abs(distance) > bandwidth)
			return 0m;

		var d = (double)(distance / bandwidth);
		var result = (Math.PI / 4d) * Math.Cos(Math.PI / 2d * d);
		return (decimal)result;
	}

	private static decimal Logistic(decimal distance, decimal bandwidth)
	{
		var d = (double)(distance / bandwidth);
		var result = 1d / (Math.Exp(d) + 2d + Math.Exp(-d));
		return (decimal)result;
	}

	private static decimal Sigmoid(decimal distance, decimal bandwidth)
	{
		var d = (double)(distance / bandwidth);
		var result = 2d / Math.PI * (1d / (Math.Exp(d) + Math.Exp(-d)));
		return (decimal)result;
	}

	private static decimal Select(string kernel, decimal distance, decimal bandwidth)
	{
		return kernel switch
		{
			"uniform" => Uniform(distance, bandwidth),
			"triangle" => Triangular(distance, bandwidth),
			"epanechnikov" => Epanechnikov(distance, bandwidth),
			"quartic" => Quartic(distance, bandwidth),
			"triweight" => Triweight(distance, bandwidth),
			"tricubic" => Tricubic(distance, bandwidth),
			"gaussian" => Gaussian(distance, bandwidth),
			"cosine" => Cosine(distance, bandwidth),
			"logistic" => Logistic(distance, bandwidth),
			"sigmoid" => Sigmoid(distance, bandwidth),
			_ => throw new ArgumentException("Invalid kernel", nameof(kernel)),
		};
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Improved McGinley Dynamic indicator.
/// Enters long when price crosses above the McGinley Dynamic line and
/// short when price crosses below.
/// </summary>
public class McGinleyDynamicImprovedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _period;
	private readonly StrategyParam<string> _formula;
	private readonly StrategyParam<decimal> _kCustom;
	private readonly StrategyParam<decimal> _exponent;
	private readonly StrategyParam<bool> _disableUmd;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Calculation period.
	/// </summary>
	public decimal Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Coefficient formula (Modern, Original, Custom).
	/// </summary>
	public string Formula { get => _formula.Value; set => _formula.Value = value; }

	/// <summary>
	/// Custom k coefficient when Formula is Custom.
	/// </summary>
	public decimal KCustom { get => _kCustom.Value; set => _kCustom.Value = value; }

	/// <summary>
	/// Exponent tweak.
	/// </summary>
	public decimal Exponent { get => _exponent.Value; set => _exponent.Value = value; }

	/// <summary>
	/// Disable plotting of the unconstrained variant.
	/// </summary>
	public bool DisableUmd { get => _disableUmd.Value; set => _disableUmd.Value = value; }

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="McGinleyDynamicImprovedStrategy"/>.
	/// </summary>
	public McGinleyDynamicImprovedStrategy()
	{
		_period = Param(nameof(Period), 14m)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_formula = Param(nameof(Formula), "Modern")
			.SetDisplay("Formulation", "Coefficient formula", "Parameters");

		_kCustom = Param(nameof(KCustom), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Custom k", "Custom k value", "Parameters");

		_exponent = Param(nameof(Exponent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Exponent", "Exponent tweak", "Parameters");

		_disableUmd = Param(nameof(DisableUmd), false)
			.SetDisplay("Disable umd()", "Hide unconstrained variant", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var k = Formula == "Modern" ? 0.6m : Formula == "Original" ? 1m : KCustom;
		var md = new ImprovedMcGinleyDynamic
		{
			Period = Period,
			K = k,
			Exponent = Exponent
		};
		var umd = new UnconstrainedMcGinleyDynamic
		{
			Period = Period,
			K = k,
			Exponent = Exponent
		};
		var ema = new ExponentialMovingAverage { Length = (int)Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(md, ema, umd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, md);
			DrawIndicator(area, ema);
			if (!DisableUmd)
				DrawIndicator(area, umd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mdValue, decimal emaValue, decimal umdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > mdValue && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < mdValue && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private class ImprovedMcGinleyDynamic : Indicator<decimal>
	{
		public decimal Period { get; set; } = 14m;
		public decimal K { get; set; } = 0.6m;
		public decimal Exponent { get; set; } = 4m;

		private decimal? _prev;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			if (_prev is null)
			{
				_prev = price;
				return new DecimalIndicatorValue(this, price, input.Time);
			}

			var prev = _prev.Value;
			if (prev == 0m)
				prev = price;

			var period = Math.Max(1m, Period);
			var denominator = Math.Min(period, Math.Max(1m, K * period * (decimal)Math.Pow((double)(price / prev), (double)Exponent)));
			var md = prev + (price - prev) / denominator;

			_prev = md;
			return new DecimalIndicatorValue(this, md, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_prev = null;
		}
	}

	private class UnconstrainedMcGinleyDynamic : Indicator<decimal>
	{
		public decimal Period { get; set; } = 14m;
		public decimal K { get; set; } = 0.6m;
		public decimal Exponent { get; set; } = 4m;

		private decimal? _prev;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			if (_prev is null)
			{
				_prev = price;
				return new DecimalIndicatorValue(this, price, input.Time);
			}

			var prev = _prev.Value;
			if (prev == 0m)
				prev = price;

			var period = Math.Max(1m, Period);
			var denominator = K * period * (decimal)Math.Pow((double)(price / prev), (double)Exponent);
			if (denominator == 0m)
				denominator = 1m;

			var md = prev + (price - prev) / denominator;

			_prev = md;
			return new DecimalIndicatorValue(this, md, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_prev = null;
		}
	}
}

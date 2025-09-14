using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy with stochastic and volatility filters.
/// </summary>
public class TrendCollectorStrategy : Strategy {
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<decimal> _stochasticUpper;
	private readonly StrategyParam<decimal> _stochasticLower;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrLimit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private StochasticOscillator _stochastic;
	private AverageTrueRange _atr;

	/// <summary>
	/// Length of the fast EMA.
	/// </summary>
	public int FastMaLength {
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA.
	/// </summary>
	public int SlowMaLength {
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Period of the stochastic oscillator.
	/// </summary>
	public int StochasticPeriod {
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for stochastic.
	/// </summary>
	public decimal StochasticUpper {
		get => _stochasticUpper.Value;
		set => _stochasticUpper.Value = value;
	}

	/// <summary>
	/// Lower threshold for stochastic.
	/// </summary>
	public decimal StochasticLower {
		get => _stochasticLower.Value;
		set => _stochasticLower.Value = value;
	}

	/// <summary>
	/// Period for ATR indicator.
	/// </summary>
	public int AtrPeriod {
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Maximum ATR value allowed to trade.
	/// </summary>
	public decimal AtrLimit {
		get => _atrLimit.Value;
		set => _atrLimit.Value = value;
	}

	/// <summary>
	/// Start hour of trading window.
	/// </summary>
	public int StartHour {
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour of trading window.
	/// </summary>
	public int EndHour {
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendCollectorStrategy"/>.
	/// </summary>
	public TrendCollectorStrategy() {
		_fastMaLength = Param(nameof(FastMaLength), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Fast EMA length", "Parameters")
		.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 204)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Slow EMA length", "Parameters")
		.SetCanOptimize(true);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Period", "Stochastic period", "Parameters")
		.SetCanOptimize(true);

		_stochasticUpper = Param(nameof(StochasticUpper), 80m)
		.SetDisplay("Stochastic Upper", "Upper stochastic level", "Parameters")
		.SetCanOptimize(true);

		_stochasticLower = Param(nameof(StochasticLower), 20m)
		.SetDisplay("Stochastic Lower", "Lower stochastic level", "Parameters")
		.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period", "Filters")
		.SetCanOptimize(true);

		_atrLimit = Param(nameof(AtrLimit), 2m)
		.SetDisplay("ATR Limit", "Maximum ATR value", "Filters")
		.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 5)
		.SetDisplay("Start Hour", "Trading start hour", "Time");

		_endHour = Param(nameof(EndHour), 24)
		.SetDisplay("End Hour", "Trading end hour", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
		base.OnReseted();

		_fastMa = default;
		_slowMa = default;
		_stochastic = default;
		_atr = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		StartProtection();

		_fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		_stochastic = new StochasticOscillator { Length = StochasticPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _atr, _stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrValue, decimal stochValue) {
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour > EndHour)
			return;

		if (atrValue > AtrLimit)
			return;

		if (Position <= 0 && fast > slow && stochValue < StochasticLower) {
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
		} else if (Position >= 0 && fast < slow && stochValue > StochasticUpper) {
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}
	}
}

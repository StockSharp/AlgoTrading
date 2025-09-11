using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Profitable pullback strategy.
/// </summary>
public class ProfitablePullbackStrategyMark804Strategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _enableSlowFilter;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Signal EMA period.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MediumLength { get => _mediumLength.Value; set => _mediumLength.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Enable slow EMA filter.
	/// </summary>
	public bool EnableSlowFilter { get => _enableSlowFilter.Value; set => _enableSlowFilter.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ProfitablePullbackStrategyMark804Strategy"/> class.
	/// </summary>
	public ProfitablePullbackStrategyMark804Strategy()
	{
		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA period", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_signalLength = Param(nameof(SignalLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA Length", "Signal EMA period", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_mediumLength = Param(nameof(MediumLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA Length", "Medium EMA period", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(30, 100, 5);

		_slowLength = Param(nameof(SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Slow EMA period", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 10);

		_enableSlowFilter = Param(nameof(EnableSlowFilter), true)
			.SetDisplay("Enable Slow EMA Filter", "Use slow EMA as trend filter", "EMA Settings");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

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
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var signalEma = new ExponentialMovingAverage { Length = SignalLength };
		var mediumEma = new ExponentialMovingAverage { Length = MediumLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, signalEma, mediumEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, signalEma);
			DrawIndicator(area, mediumEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal signal, decimal medium, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose == 0m)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var uptrend = fast > signal && signal > medium && (!EnableSlowFilter || medium > slow);
		var downtrend = fast < signal && signal < medium && (!EnableSlowFilter || medium < slow);

		var pullbackBuy = uptrend && _prevClose < signal && candle.ClosePrice > signal;
		var pullbackSell = downtrend && _prevClose > signal && candle.ClosePrice < signal;

		if (pullbackBuy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (pullbackSell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
	}
}

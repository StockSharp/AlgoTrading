using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FmOne Scalping Strategy combining EMA crossover and MACD confirmation.
/// </summary>
public class FmOneScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private MovingAverageConvergenceDivergence _macd;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _barsSinceTrade;
	private bool _isInitialized;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FmOneScalpingStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
			.SetRange(1, 100)
			.SetDisplay("Fast EMA Period", "Period for fast EMA", "Indicators")
			;

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 26)
			.SetRange(1, 200)
			.SetDisplay("Slow EMA Period", "Period for slow EMA", "Indicators")
			;

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetRange(1, 50)
			.SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")
			;

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss as percent of entry price", "Risk")
			;

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Take Profit %", "Take profit as percent of entry price", "Risk")
			;

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Trailing Stop", "Enable trailing stop", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
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

		_fastMa = default;
		_slowMa = default;
		_macd = default;
		_prevFast = 0m;
		_prevSlow = 0m;
		_barsSinceTrade = CooldownBars;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_macd = new MovingAverageConvergenceDivergence(
			new ExponentialMovingAverage { Length = SlowMaPeriod },
			new ExponentialMovingAverage { Length = FastMaPeriod });
		_barsSinceTrade = CooldownBars;
		_isInitialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _macd, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: EnableTrailingStop
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		var macdArea = CreateChartArea();
		if (macdArea != null)
			DrawIndicator(macdArea, _macd);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceTrade++;

		if (!_isInitialized)
		{
			_prevFast = fastMa;
			_prevSlow = slowMa;
			_isInitialized = true;
			return;
		}

		var longSignal = _prevFast <= _prevSlow && fastMa > slowMa && macdValue > 0m;
		var shortSignal = _prevFast >= _prevSlow && fastMa < slowMa && macdValue < 0m;

		if (longSignal && Position <= 0 && _barsSinceTrade >= CooldownBars)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceTrade = 0;
		}
		else if (shortSignal && Position >= 0 && _barsSinceTrade >= CooldownBars)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceTrade = 0;
		}

		_prevFast = fastMa;
		_prevSlow = slowMa;
	}
}

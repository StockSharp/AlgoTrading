using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines moving-average entries with a trailing-stop exit.
/// </summary>
public class TrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailing;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _prevFastMa;
	private decimal _prevSlowMa;
	private bool _isInitialized;
	private int _barsSinceExit;

	/// <summary>
	/// Profit target distance from entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Bars to wait after a full exit before re-entering.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopStrategy"/> class.
	/// </summary>
	public TrailingStopStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 3500m)
			.SetDisplay("Take Profit", "Profit distance in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1200m)
			.SetDisplay("Stop Loss", "Loss distance in price units", "Risk");

		_trailing = Param(nameof(Trailing), 800m)
			.SetDisplay("Trailing", "Trailing stop distance", "Risk");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetDisplay("Fast MA", "Fast moving average period", "Indicator");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 18)
			.SetDisplay("Slow MA", "Slow moving average period", "Indicator");

		_cooldownBars = Param(nameof(CooldownBars), 1)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for price updates", "General");
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

		_fastMa = null;
		_slowMa = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_prevFastMa = 0m;
		_prevSlowMa = 0m;
		_isInitialized = false;
		_barsSinceExit = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		Indicators.Add(_fastMa);
		Indicators.Add(_slowMa);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, price, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, price, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevFastMa = fastValue;
			_prevSlowMa = slowValue;
			_isInitialized = true;
			return;
		}

		if (Position != 0)
		{
			_prevFastMa = fastValue;
			_prevSlowMa = slowValue;
			return;
		}

		var crossUp = _prevFastMa <= _prevSlowMa && fastValue > slowValue;
		var crossDown = _prevFastMa >= _prevSlowMa && fastValue < slowValue;

		if (crossUp)
			BuyMarket();
		else if (crossDown)
			SellMarket();

		_prevFastMa = fastValue;
		_prevSlowMa = slowValue;
	}
}

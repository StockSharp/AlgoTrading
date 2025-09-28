namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// EMA(6) and EMA(12) crossover strategy with optional trailing stop.
/// </summary>
public class Ema612Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<bool> _useCloseSignals;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _shortEma = null!;
	private EMA _longEma = null!;
	private decimal? _previousShort;
	private decimal? _previousLong;
	private bool _orderPending;
	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of <see cref="Ema612Strategy"/>.
	/// </summary>
	public Ema612Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base order size in lots.", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 10m, 0.1m);

		_shortEmaLength = Param(nameof(ShortEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Length of the fast exponential moving average.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 40, 1);

		_longEmaLength = Param(nameof(LongEmaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Length of the slow exponential moving average.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(6, 120, 2);

		_useCloseSignals = Param(nameof(UseCloseSignals), true)
			.SetDisplay("Use Close Signals", "Close the current position on opposite EMA cross.", "Trading Rules");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (steps)", "Trailing stop distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 1000m)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 2000m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to calculate moving averages.", "General");
	}

	/// <summary>
	/// Base order size expressed in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Length of the fast exponential moving average.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow exponential moving average.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Close open positions when an opposite crossing appears.
	/// </summary>
	public bool UseCloseSignals
	{
		get => _useCloseSignals.Value;
		set => _useCloseSignals.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps. Zero disables the trailing behaviour.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps. Zero disables the take profit.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate EMA values.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security != null)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_shortEma = null!;
		_longEma = null!;
		_previousShort = null;
		_previousLong = null;
		_orderPending = false;
		_priceStep = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		Volume = AlignVolume(OrderVolume);

		var takeProfitUnit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps * _priceStep, UnitTypes.Absolute) : null;
		var trailingUnit = TrailingStopSteps > 0m ? new Unit(TrailingStopSteps * _priceStep, UnitTypes.Absolute) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: trailingUnit,
			useMarketOrders: true);

		_shortEma = new EMA { Length = ShortEmaLength };
		_longEma = new EMA { Length = LongEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
	{
		base.OnOrderRegisterFailed(fail, calcRisk);

		_orderPending = false;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_previousShort = null;
			_previousLong = null;
		}

		_orderPending = false;
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEmaValue, decimal longEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prevShort = _previousShort;
		var prevLong = _previousLong;

		_previousShort = shortEmaValue;
		_previousLong = longEmaValue;

		if (prevShort is null || prevLong is null)
			return;

		var previousDiff = prevShort.Value - prevLong.Value;
		var currentDiff = shortEmaValue - longEmaValue;

		var crossedUp = previousDiff <= 0m && currentDiff > 0m;
		var crossedDown = previousDiff >= 0m && currentDiff < 0m;

		if (!crossedUp && !crossedDown)
			return;

		if (_orderPending)
			return;

		if (crossedUp)
		{
			if (UseCloseSignals && Position < 0m)
			{
				var volumeToClose = Math.Abs(Position);

				if (volumeToClose > 0m)
				{
					BuyMarket(volumeToClose);
					_orderPending = true;
				}

				return;
			}

			if (Position < 0m)
				return;

			if (Position == 0m)
			{
				BuyMarket(Volume);
				_orderPending = true;
			}

			return;
		}

		if (crossedDown)
		{
			if (UseCloseSignals && Position > 0m)
			{
				var volumeToClose = Position;

				if (volumeToClose > 0m)
				{
					SellMarket(volumeToClose);
					_orderPending = true;
				}

				return;
			}

			if (Position > 0m)
				return;

			if (Position == 0m)
			{
				SellMarket(Volume);
				_orderPending = true;
			}
		}
	}
}


using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR trader strategy implementing basic reversal logic.
/// </summary>
public class PsarTraderV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maximum;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;

	private decimal _prevSar;
	private bool _prevPriceAboveSar;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Candle type for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Acceleration step for PSAR indicator.
	/// </summary>
	public decimal Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}

	/// <summary>
	/// Maximum acceleration for PSAR indicator.
	/// </summary>
	public decimal Maximum
	{
		get => _maximum.Value;
		set => _maximum.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Close opposite position when new signal appears.
	/// </summary>
	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="PsarTraderV2Strategy"/>.
	/// </summary>
	public PsarTraderV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Trading timeframe", "General");

		_step = Param(nameof(Step), 0.001m)
			.SetDisplay("PSAR Step", "Acceleration step for PSAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);

		_maximum = Param(nameof(Maximum), 0.2m)
			.SetDisplay("PSAR Maximum", "Maximum acceleration for PSAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Profit target in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetDisplay("Stop Loss", "Loss limit in price units", "Risk");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading start hour", "Time");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading end hour", "Time");

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), true)
			.SetDisplay("Close Opposite", "Close opposite position on signal", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSar = 0m;
		_prevPriceAboveSar = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var psar = new ParabolicSar
		{
			AccelerationStep = Step,
			AccelerationMax = Maximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(psar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, psar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour > EndHour)
			return;

		var priceAboveSar = candle.ClosePrice > sar;
		var cross = _prevSar != 0m && priceAboveSar != _prevPriceAboveSar;

		if (cross)
		{
			var volume = Volume + Math.Abs(Position);

			if (priceAboveSar && Position <= 0)
			{
				if (CloseOnOppositeSignal && Position < 0)
					ClosePosition();

				BuyMarket(volume);

				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				_takePrice = _entryPrice + TakeProfit;
			}
			else if (!priceAboveSar && Position >= 0)
			{
				if (CloseOnOppositeSignal && Position > 0)
					ClosePosition();

				SellMarket(volume);

				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				_takePrice = _entryPrice - TakeProfit;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				ClosePosition();
		}

		_prevSar = sar;
		_prevPriceAboveSar = priceAboveSar;
	}
}

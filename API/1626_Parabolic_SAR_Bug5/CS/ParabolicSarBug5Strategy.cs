using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with optional reversal and trailing stop.
/// Based on conversion of MQL script pSAR_bug_5.
/// </summary>
public class ParabolicSarBug5Strategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maximum;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _trailing;
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _sarClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private bool _prevAbove;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal Step { get => _step.Value; set => _step.Value = value; }

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal Maximum { get => _maximum.Value; set => _maximum.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool Trailing { get => _trailing.Value; set => _trailing.Value = value; }

	/// <summary>
	/// Trailing distance in points.
	/// </summary>
	public decimal TrailPoints { get => _trailPoints.Value; set => _trailPoints.Value = value; }

	/// <summary>
	/// Reverse trading direction.
	/// </summary>
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }

	/// <summary>
	/// Close position on SAR switch.
	/// </summary>
	public bool SarClose { get => _sarClose.Value; set => _sarClose.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ParabolicSarBug5Strategy()
	{
		_step = Param(nameof(Step), 0.001m)
			.SetDisplay("Step", "Initial acceleration factor", "Indicators");

		_maximum = Param(nameof(Maximum), 0.2m)
			.SetDisplay("Maximum", "Maximum acceleration factor", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 90m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_trailing = Param(nameof(Trailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailPoints = Param(nameof(TrailPoints), 10m)
			.SetDisplay("Trail Points", "Trailing distance in points", "Risk");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Reverse trading direction", "General");

		_sarClose = Param(nameof(SarClose), true)
			.SetDisplay("SAR Close", "Close position on SAR switch", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSar = 0m;
		_prevAbove = false;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var psar = new ParabolicSar
		{
			Acceleration = Step,
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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceAbove = candle.ClosePrice > sar;
		var crossing = _prevSar > 0 && priceAbove != _prevAbove;

		if (crossing)
		{
			var isBuySignal = priceAbove;
			if (Reverse)
				isBuySignal = !isBuySignal;

			if (SarClose && Position != 0)
				ClosePosition();

			var volume = Volume + Math.Abs(Position);

			if (isBuySignal && Position <= 0)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_takePrice = _entryPrice + TakeProfitPoints;
				_stopPrice = _entryPrice - StopLossPoints;
				_highestPrice = candle.HighPrice;
			}
			else if (!isBuySignal && Position >= 0)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_takePrice = _entryPrice - TakeProfitPoints;
				_stopPrice = _entryPrice + StopLossPoints;
				_lowestPrice = candle.LowPrice;
			}
		}

		if (Position > 0 && _entryPrice is decimal)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			if (Trailing)
				_stopPrice = Math.Max(_stopPrice ?? 0m, _highestPrice - TrailPoints);

			if (_stopPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ResetState();
			}
			else if (_takePrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ResetState();
			}
		}
		else if (Position < 0 && _entryPrice is decimal)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			if (Trailing)
				_stopPrice = Math.Min(_stopPrice ?? decimal.MaxValue, _lowestPrice + TrailPoints);

			if (_stopPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ResetState();
			}
			else if (_takePrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ResetState();
			}
		}

		_prevSar = sar;
		_prevAbove = priceAbove;
	}

	private void ResetState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}
}


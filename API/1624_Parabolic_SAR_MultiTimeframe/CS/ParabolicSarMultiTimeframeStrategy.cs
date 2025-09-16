using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe Parabolic SAR strategy.
/// Uses four timeframes of Parabolic SAR to confirm trend direction.
/// Opens long when price is above all SARs and short when below.
/// </summary>
public class ParabolicSarMultiTimeframeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step15;
	private readonly StrategyParam<decimal> _step30;
	private readonly StrategyParam<decimal> _step60;
	private readonly StrategyParam<decimal> _step240;
	private readonly StrategyParam<decimal> _maxStep;

	private readonly DataType _tf15 = TimeSpan.FromMinutes(15).TimeFrame();
	private readonly DataType _tf30 = TimeSpan.FromMinutes(30).TimeFrame();
	private readonly DataType _tf60 = TimeSpan.FromHours(1).TimeFrame();
	private readonly DataType _tf240 = TimeSpan.FromHours(4).TimeFrame();

	private decimal? _sar15;
	private decimal? _sar30;
	private decimal? _sar60;
	private decimal? _sar240;

	/// <summary>
	/// Step value for 15-minute Parabolic SAR.
	/// </summary>
	public decimal Step15
	{
		get => _step15.Value;
		set => _step15.Value = value;
	}

	/// <summary>
	/// Step value for 30-minute Parabolic SAR.
	/// </summary>
	public decimal Step30
	{
		get => _step30.Value;
		set => _step30.Value = value;
	}

	/// <summary>
	/// Step value for 1-hour Parabolic SAR.
	/// </summary>
	public decimal Step60
	{
		get => _step60.Value;
		set => _step60.Value = value;
	}

	/// <summary>
	/// Step value for 4-hour Parabolic SAR.
	/// </summary>
	public decimal Step240
	{
		get => _step240.Value;
		set => _step240.Value = value;
	}

	/// <summary>
	/// Maximum step for all SAR indicators.
	/// </summary>
	public decimal MaxStep
	{
		get => _maxStep.Value;
		set => _maxStep.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ParabolicSarMultiTimeframeStrategy()
	{
		_step15 = Param(nameof(Step15), 0.062m)
			.SetDisplay("Step15", "SAR acceleration for 15 minute timeframe", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_step30 = Param(nameof(Step30), 0.058m)
			.SetDisplay("Step30", "SAR acceleration for 30 minute timeframe", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_step60 = Param(nameof(Step60), 0.058m)
			.SetDisplay("Step60", "SAR acceleration for 1 hour timeframe", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_step240 = Param(nameof(Step240), 0.058m)
			.SetDisplay("Step240", "SAR acceleration for 4 hour timeframe", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_maxStep = Param(nameof(MaxStep), 0.1m)
			.SetDisplay("MaxStep", "Maximum SAR acceleration", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, _tf15), (Security, _tf30), (Security, _tf60), (Security, _tf240)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sar15 = new ParabolicSar { Acceleration = Step15, AccelerationMax = MaxStep };
		var sar30 = new ParabolicSar { Acceleration = Step30, AccelerationMax = MaxStep };
		var sar60 = new ParabolicSar { Acceleration = Step60, AccelerationMax = MaxStep };
		var sar240 = new ParabolicSar { Acceleration = Step240, AccelerationMax = MaxStep };

		var sub15 = SubscribeCandles(_tf15);
		sub15.Bind(sar15, Process15).Start();

		var sub30 = SubscribeCandles(_tf30);
		sub30.Bind(sar30, Process30).Start();

		var sub60 = SubscribeCandles(_tf60);
		sub60.Bind(sar60, Process60).Start();

		var sub240 = SubscribeCandles(_tf240);
		sub240.Bind(sar240, Process240).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub15);
			DrawIndicator(area, sar15);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void Process30(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sar30 = sar;
	}

	private void Process60(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sar60 = sar;
	}

	private void Process240(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sar240 = sar;
	}

	private void Process15(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sar15 = sar;

		if (_sar30 is null || _sar60 is null || _sar240 is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var volume = Volume + Math.Abs(Position);

		var allBull = price > _sar15 && price > _sar30 && price > _sar60 && price > _sar240;
		var allBear = price < _sar15 && price < _sar30 && price < _sar60 && price < _sar240;

		if (allBull && Position <= 0)
		{
			BuyMarket(volume);
			LogInfo($"Buy: price {price} above SARs {_sar15}/{_sar30}/{_sar60}/{_sar240}");
		}
		else if (allBear && Position >= 0)
		{
			SellMarket(volume);
			LogInfo($"Sell: price {price} below SARs {_sar15}/{_sar30}/{_sar60}/{_sar240}");
		}
		else if ((Position > 0 && allBear) || (Position < 0 && allBull))
		{
			ClosePosition();
		}
	}
}

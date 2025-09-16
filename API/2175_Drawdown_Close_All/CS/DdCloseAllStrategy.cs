using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that closes all positions when drawdown exceeds a specified percentage.
/// </summary>
public class DdCloseAllStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxDrawdownPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _peakEquity;
	private bool _drawdownTriggered;

	/// <summary>
	/// Maximum allowed drawdown in percent. 100 disables the strategy.
	/// </summary>
	public decimal MaxDrawdownPercent
	{
		get => _maxDrawdownPercent.Value;
		set => _maxDrawdownPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic drawdown checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DdCloseAllStrategy()
	{
		_maxDrawdownPercent = Param(nameof(MaxDrawdownPercent), 100m)
			.SetRange(0m, 100m)
			.SetDisplay("Max Drawdown (%)", "Drawdown percentage to trigger closing positions", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for drawdown check", "General");
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

		_peakEquity = 0m;
		_drawdownTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_peakEquity = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDrawdown();
	}

	private void UpdateDrawdown()
	{
		var equity = Portfolio.CurrentValue ?? 0m;
		if (equity > _peakEquity)
		{
			_peakEquity = equity;
			_drawdownTriggered = false;
		}

		if (_peakEquity <= 0m)
			return;

		var dd = (_peakEquity - equity) / _peakEquity * 100m;
		if (!_drawdownTriggered && dd >= MaxDrawdownPercent && MaxDrawdownPercent < 100m)
		{
			_drawdownTriggered = true;
			CloseAll("Drawdown limit reached");
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes the current position when portfolio profit exceeds a percentage of initial capital.
/// </summary>
public class YurazCloseprcV3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _initialCapital;
	private bool _isClosed;

	/// <summary>
	/// Profit percentage threshold for closing the position.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger profit checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public YurazCloseprcV3Strategy()
	{
		_profitPercent = Param(nameof(ProfitPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Profit %", "Profit percentage threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for checks", "General");
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
		_initialCapital = default;
		_isClosed = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialCapital = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var equity = Portfolio.CurrentValue ?? 0m;
		if (_initialCapital <= 0m)
			return;

		var profitPercent = (equity - _initialCapital) / _initialCapital * 100m;

		if (profitPercent >= ProfitPercent && Position != 0 && !_isClosed)
		{
			// Close current position at market price
			ClosePosition();
			_isClosed = true;
		}
	}
}

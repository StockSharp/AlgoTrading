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
/// Closes the current position when floating PnL reaches a profit target or maximum loss.
/// Simplified from the multi-pair closer utility to work with a single security.
/// </summary>
public class MultiPairCloserStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _minAgeSeconds;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;

	private SimpleMovingAverage _sma;
	private decimal _entryPrice;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Profit target in price units.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum tolerated loss in price units.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Minimum age of an open position in seconds before exit is permitted.
	/// </summary>
	public int MinAgeSeconds
	{
		get => _minAgeSeconds.Value;
		set => _minAgeSeconds.Value = value;
	}

	/// <summary>
	/// Candle type for price monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// SMA period for entry signals.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MultiPairCloserStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 5m)
			.SetNotNegative()
			.SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management");

		_maxLoss = Param(nameof(MaxLoss), 10m)
			.SetNotNegative()
			.SetDisplay("Maximum Loss", "Close position when floating loss reaches this value", "Risk Management");

		_minAgeSeconds = Param(nameof(MinAgeSeconds), 60)
			.SetNotNegative()
			.SetDisplay("Min Age (s)", "Minimum holding time before exit is allowed", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for monitoring", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Moving average period for entry signal", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_entryPrice = 0m;
		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = SmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var price = candle.ClosePrice;
		var time = candle.CloseTime;

		// Check exit conditions for open position
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			var canClose = MinAgeSeconds <= 0 ||
				(_entryTime.HasValue && (time - _entryTime.Value).TotalSeconds >= MinAgeSeconds);

			if (canClose)
			{
				if ((ProfitTarget > 0m && pnl >= ProfitTarget) ||
					(MaxLoss > 0m && pnl <= -MaxLoss))
				{
					if (Position > 0)
						SellMarket(Math.Abs(Position));
					else
						BuyMarket(Math.Abs(Position));

					_entryPrice = 0m;
					_entryTime = null;
					return;
				}
			}
		}

		// Entry logic: trend following with SMA
		if (Position == 0)
		{
			if (price > smaValue)
			{
				BuyMarket();
				_entryPrice = price;
				_entryTime = time;
			}
			else if (price < smaValue)
			{
				SellMarket();
				_entryPrice = price;
				_entryTime = time;
			}
		}
	}
}

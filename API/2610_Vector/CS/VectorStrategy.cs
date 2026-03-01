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
/// Trend strategy using smoothed moving averages. Simplified from multi-currency Vector.
/// </summary>
public class VectorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _fastMa;
	private SmoothedMovingAverage _slowMa;
	private decimal _entryPrice;
	private decimal _initialBalance;
	private int _processedBars;

	/// <summary>
	/// Fast smoothed moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothed moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Additional warm-up shift in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Floating profit target percent of balance.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Floating loss limit percent of balance.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VectorStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast smoothed moving average period", "Indicators")
			.SetOptimize(3, 15, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow smoothed moving average period", "Indicators")
			.SetOptimize(5, 25, 1);

		_maShift = Param(nameof(MaShift), 8)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Additional warm-up bars before signals", "Indicators");

		_profitPercent = Param(nameof(ProfitPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Equity TP %", "Close all when floating profit reaches this percent", "Risk");

		_lossPercent = Param(nameof(LossPercent), 30m)
			.SetNotNegative()
			.SetDisplay("Equity SL %", "Close all when floating loss reaches this percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Timeframe", "Timeframe for moving averages", "General");
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
		_fastMa = null;
		_slowMa = null;
		_entryPrice = 0m;
		_initialBalance = 0m;
		_processedBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new SmoothedMovingAverage { Length = FastMaPeriod };
		_slowMa = new SmoothedMovingAverage { Length = SlowMaPeriod };
		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		SubscribeCandles(CandleType)
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_processedBars++;

		if (!IsFormed)
			return;

		if (_processedBars <= MaShift)
			return;

		// Check equity thresholds
		if (Position != 0 && _initialBalance > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			var floating = equity - _initialBalance;
			var profitThreshold = _initialBalance * ProfitPercent / 100m;
			var lossThreshold = _initialBalance * LossPercent / 100m;

			if ((profitThreshold > 0m && floating >= profitThreshold) ||
				(lossThreshold > 0m && floating <= -lossThreshold))
			{
				if (Position > 0) SellMarket(Math.Abs(Position));
				else if (Position < 0) BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				return;
			}
		}

		// Entry/exit logic
		if (fastValue > slowValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (fastValue < slowValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}
}

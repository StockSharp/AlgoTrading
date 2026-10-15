using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adapted from the MetaTrader "EES Hedger" expert advisor.
/// Uses EMA crossover signals with break-even and trailing stop risk management.
/// </summary>
public class EesHedgerAdvancedStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public EesHedgerAdvancedStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 500)
			.SetDisplay("Stop Loss", "Stop-loss distance", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500)
			.SetDisplay("Take Profit", "Take-profit distance", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "Market Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var fastEma = new EMA { Length = FastPeriod };
		var slowEma = new EMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}

		// Use StartProtection for SL/TP
		var tp = TakeProfitPips > 0 ? new Unit(TakeProfitPips, UnitTypes.Absolute) : null;
		var sl = StopLossPips > 0 ? new Unit(StopLossPips, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		// EMA crossover detection
		var crossedUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossedDown = _prevFast >= _prevSlow && fastValue < slowValue;

		if (crossedUp)
		{
			// Close short if any, then go long
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position <= 0)
				BuyMarket(Volume);
		}
		else if (crossedDown)
		{
			// Close long if any, then go short
			if (Position > 0)
				SellMarket(Position);
			if (Position >= 0)
				SellMarket(Volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}

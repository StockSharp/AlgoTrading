using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Escort Trend strategy combining WMA crossover with CCI confirmation.
/// Buys when fast WMA above slow WMA and CCI above threshold.
/// Sells when opposite conditions met.
/// </summary>
public class EscortTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastWmaPeriod;
	private readonly StrategyParam<int> _slowWmaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastWmaPeriod { get => _fastWmaPeriod.Value; set => _fastWmaPeriod.Value = value; }
	public int SlowWmaPeriod { get => _slowWmaPeriod.Value; set => _slowWmaPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciThreshold { get => _cciThreshold.Value; set => _cciThreshold.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EscortTrendStrategy()
	{
		_fastWmaPeriod = Param(nameof(FastWmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Length of fast weighted MA", "General");

		_slowWmaPeriod = Param(nameof(SlowWmaPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Length of slow weighted MA", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "General");

		_cciThreshold = Param(nameof(CciThreshold), 100m)
			.SetDisplay("CCI Threshold", "Threshold for CCI signal", "General");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastWma = new WeightedMovingAverage { Length = FastWmaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowWmaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, cci, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Buy crossover: fast WMA crosses above slow WMA with CCI confirmation
		var crossUp = _prevFast <= _prevSlow && fast > slow && cciValue > CciThreshold;
		// Sell crossover: fast WMA crosses below slow WMA with CCI confirmation
		var crossDown = _prevFast >= _prevSlow && fast < slow && cciValue < -CciThreshold;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

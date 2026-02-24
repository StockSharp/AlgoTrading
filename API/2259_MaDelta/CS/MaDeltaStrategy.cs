using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Delta strategy. Trades when the cubed amplified difference
/// between fast and slow moving averages crosses dynamic thresholds.
/// </summary>
public class MaDeltaStrategy : Strategy
{
	private readonly StrategyParam<int> _delta;
	private readonly StrategyParam<int> _multiplier;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _hi;
	private decimal _lo;
	private bool _isInit;
	private int _trade;
	private decimal _deltaStep;
	private decimal _multiplierFactor;

	public int Delta { get => _delta.Value; set => _delta.Value = value; }
	public int Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaDeltaStrategy()
	{
		_delta = Param(nameof(Delta), 195)
			.SetDisplay("Delta (pips)", "Hi-Lo threshold in pips", "General")
			.SetOptimize(50, 300, 5);

		_multiplier = Param(nameof(Multiplier), 392)
			.SetDisplay("Multiplier", "Amplifier for MA difference", "General")
			.SetOptimize(100, 500, 10);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 26)
			.SetDisplay("Fast MA Period", "Period for fast moving average", "Indicators")
			.SetOptimize(5, 50, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 51)
			.SetDisplay("Slow MA Period", "Period for slow moving average", "Indicators")
			.SetOptimize(10, 100, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hi = 0m;
		_lo = 0m;
		_isInit = false;
		_trade = 0;

		_deltaStep = Delta * 0.00001m;
		_multiplierFactor = Multiplier * 0.1m;

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = _multiplierFactor * (fastMaValue - slowMaValue);
		var px = (decimal)Math.Pow((double)diff, 3);

		if (!_isInit)
		{
			_hi = 0m;
			_lo = 0m;
			_trade = 0;
			_isInit = true;
		}

		if (px > _hi)
		{
			_hi = px;
			_lo = _hi - _deltaStep;
			_trade = 1;
		}
		else if (px < _lo)
		{
			_lo = px;
			_hi = _lo + _deltaStep;
			_trade = -1;
		}

		if (_trade == 1 && Position <= 0)
			BuyMarket();
		else if (_trade == -1 && Position >= 0)
			SellMarket();
	}
}

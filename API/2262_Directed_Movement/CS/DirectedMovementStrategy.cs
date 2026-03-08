using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directed Movement Strategy - RSI cross system with two MA smoothing.
/// </summary>
public class DirectedMovementStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }

	public DirectedMovementStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_fastMaLength = Param(nameof(FastMaLength), 12)
			.SetDisplay("Fast MA Length", "Period of fast moving average", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 5)
			.SetDisplay("Slow MA Length", "Period of slow moving average", "Indicators");
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
		_fastMa = null;
		_slowMa = null;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0m;
		_prevSlow = 0m;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaLength };

		Indicators.Add(_fastMa);
		Indicators.Add(_slowMa);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.ServerTime;

		var fastResult = _fastMa.Process(new DecimalIndicatorValue(_fastMa, rsiValue, t) { IsFinal = true });
		if (!_fastMa.IsFormed)
			return;

		var fast = fastResult.GetValue<decimal>();

		var slowResult = _slowMa.Process(new DecimalIndicatorValue(_slowMa, fast, t) { IsFinal = true });
		if (!_slowMa.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = fast;
			return;
		}

		var slow = slowResult.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// Crossover: fast crosses below slow -> buy
		if (_prevFast > _prevSlow && fast <= slow && Position <= 0)
			BuyMarket();
		// Crossover: fast crosses above slow -> sell
		else if (_prevFast < _prevSlow && fast >= slow && Position >= 0)
			SellMarket();

		_prevFast = fast;
		_prevSlow = slow;
	}
}

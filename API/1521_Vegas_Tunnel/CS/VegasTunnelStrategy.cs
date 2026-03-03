using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vegas Tunnel strategy using EMA tunnel crossover.
/// Goes long when fast EMA crosses above the tunnel, short when below.
/// Uses StdDev-based stops and risk/reward targets.
/// </summary>
public class VegasTunnelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _stdVal;
	private decimal _prevSlow;
	private decimal _prevTunnel;
	private int _cooldown;

	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VegasTunnelStrategy()
	{
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "General");

		_stopMult = Param(nameof(StopMult), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev multiplier for stop", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_stopPrice = 0;
		_takePrice = 0;
		_stdVal = 0;
		_prevSlow = 0;
		_prevTunnel = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaSlow = new ExponentialMovingAverage { Length = 144 };
		var emaTunnel = new ExponentialMovingAverage { Length = 169 };
		var stdDev = new StandardDeviation { Length = 20 };

		_stopPrice = 0;
		_takePrice = 0;
		_stdVal = 0;
		_prevSlow = 0;
		_prevTunnel = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(stdDev, (candle, val) => _stdVal = val)
			.Bind(emaSlow, emaTunnel, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, emaTunnel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slow, decimal tunnel)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldown > 0)
			_cooldown--;

		if (_stdVal <= 0)
		{
			_prevSlow = slow;
			_prevTunnel = tunnel;
			return;
		}

		// Exit management
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_stopPrice = 0;
				_takePrice = 0;
				_cooldown = 80;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_stopPrice = 0;
				_takePrice = 0;
				_cooldown = 80;
			}
		}

		if (_cooldown > 0 || _prevSlow == 0)
		{
			_prevSlow = slow;
			_prevTunnel = tunnel;
			return;
		}

		// Entry: slow EMA (144) crosses tunnel EMA (169)
		var slowCrossAboveTunnel = _prevSlow <= _prevTunnel && slow > tunnel;
		var slowCrossBelowTunnel = _prevSlow >= _prevTunnel && slow < tunnel;

		if (slowCrossAboveTunnel && candle.ClosePrice > tunnel && Position <= 0)
		{
			BuyMarket();
			var entry = candle.ClosePrice;
			_stopPrice = entry - StopMult * _stdVal;
			_takePrice = entry + (entry - _stopPrice) * RiskRewardRatio;
			_cooldown = 80;
		}
		else if (slowCrossBelowTunnel && candle.ClosePrice < tunnel && Position >= 0)
		{
			SellMarket();
			var entry = candle.ClosePrice;
			_stopPrice = entry + StopMult * _stdVal;
			_takePrice = entry - (_stopPrice - entry) * RiskRewardRatio;
			_cooldown = 80;
		}

		_prevSlow = slow;
		_prevTunnel = tunnel;
	}
}

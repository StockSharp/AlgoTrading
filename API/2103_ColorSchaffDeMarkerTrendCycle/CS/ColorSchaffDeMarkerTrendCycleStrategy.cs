using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Schaff DeMarker Trend Cycle oscillator.
/// Uses fast/slow DeMarker difference with double stochastic smoothing.
/// </summary>
public class ColorSchaffDeMarkerTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSt;
	private decimal _prevStc;
	private bool _st1Pass;
	private bool _st2Pass;
	private int _prevColor;
	private readonly List<decimal> _macdBuf = new();
	private readonly List<decimal> _stBuf = new();

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int Cycle { get => _cycle.Value; set => _cycle.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorSchaffDeMarkerTrendCycleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast DeMarker", "Fast DeMarker period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow DeMarker", "Slow DeMarker period", "Indicator");

		_cycle = Param(nameof(Cycle), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cycle", "Cycle length", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper threshold", "Levels");

		_lowLevel = Param(nameof(LowLevel), -60m)
			.SetDisplay("Low Level", "Lower threshold", "Levels");

		_factor = Param(nameof(Factor), 0.5m)
			.SetDisplay("Factor", "Smoothing factor", "Indicator");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevSt = 0; _prevStc = 0;
		_st1Pass = false; _st2Pass = false;
		_prevColor = 0;
		_macdBuf.Clear();
		_stBuf.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new DeMarker { Length = FastPeriod };
		var slow = new DeMarker { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, (candle, fastVal, slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var macd = fastVal - slowVal;

				// Track high/low of macd manually
				_macdBuf.Add(macd);
				if (_macdBuf.Count > Cycle) _macdBuf.RemoveAt(0);
				if (_macdBuf.Count < Cycle) return;

				var macdHigh = decimal.MinValue;
				var macdLow = decimal.MaxValue;
				foreach (var v in _macdBuf)
				{
					if (v > macdHigh) macdHigh = v;
					if (v < macdLow) macdLow = v;
				}

				decimal st;
				if (macdHigh == macdLow) st = _prevSt;
				else st = (macd - macdLow) / (macdHigh - macdLow) * 100m;

				if (_st1Pass) st = Factor * (st - _prevSt) + _prevSt;
				_prevSt = st;
				_st1Pass = true;

				_stBuf.Add(st);
				if (_stBuf.Count > Cycle) _stBuf.RemoveAt(0);
				if (_stBuf.Count < Cycle) return;

				var stHigh = decimal.MinValue;
				var stLow = decimal.MaxValue;
				foreach (var v in _stBuf)
				{
					if (v > stHigh) stHigh = v;
					if (v < stLow) stLow = v;
				}

				decimal stc;
				if (stHigh == stLow) stc = _prevStc;
				else stc = (st - stLow) / (stHigh - stLow) * 200m - 100m;

				if (_st2Pass) stc = Factor * (stc - _prevStc) + _prevStc;
				var dStc = stc - _prevStc;
				_prevStc = stc;
				_st2Pass = true;

				int color;
				if (stc > 0) color = stc > HighLevel ? (dStc >= 0 ? 7 : 6) : (dStc >= 0 ? 5 : 4);
				else color = stc < LowLevel ? (dStc < 0 ? 0 : 1) : (dStc < 0 ? 2 : 3);

				if (_prevColor > 5 && color < 6 && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				else if (_prevColor < 2 && color > 1 && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}

				_prevColor = color;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}

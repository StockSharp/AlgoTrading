using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the VininI Trend concept using the CCI indicator.
/// Buys when CCI breaks above upper level, sells when CCI breaks below lower level.
/// </summary>
public class VininITrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;
	private readonly StrategyParam<int> _mode;
	private Order _pending;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public int UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public int DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public int Mode { get => _mode.Value; set => _mode.Value = value; }
	public VininITrendStrategy()
	{
		_mode = Param(nameof(Mode), 0).SetRange(0, 1).SetDisplay("Entry Mode", "0: Breakdown, 1: Twist", "Parameters");
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for the CCI indicator", "Parameters");

		_upLevel = Param(nameof(UpLevel), 10)
			.SetDisplay("Upper Level", "Upper threshold to trigger buy", "Parameters");

		_downLevel = Param(nameof(DownLevel), -10)
			.SetDisplay("Lower Level", "Lower threshold to trigger sell", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prev1 = default;
		_prev2 = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = _prev2 = null;
		_pending = null;

		var cci = new CommodityChannelIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!cciValue.IsFormed)
			return;

		var cci = cciValue.ToDecimal();

		var buy = false;
		var sell = false;
		if (_prev1 is decimal previous)
		{
			if (Mode == 0)
			{
				buy = previous <= UpLevel && cci > UpLevel;
				sell = previous >= DownLevel && cci < DownLevel;
			}
			else if (_prev2 is decimal older)
			{
				buy = previous < older && cci > previous;
				sell = previous > older && cci < previous;
			}
		}
		if (IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (buy && Position <= 0m) _pending = BuyMarket(Volume - Math.Min(0m, Position));
			else if (sell && Position >= 0m) _pending = SellMarket(Volume + Math.Max(0m, Position));
		}

		_prev2 = _prev1;
		_prev1 = cci;
	}
}

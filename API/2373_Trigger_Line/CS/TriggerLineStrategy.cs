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
/// Trigger Line cross strategy based on weighted trend line and LSMA.
/// </summary>
public class TriggerLineStrategy : Strategy
{
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _lsmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _wma;
	private LinearReg _lsma;

	private bool _initialized;
	private decimal _prevLine;
	private decimal _prevSignal;

	public TriggerLineStrategy()
	{
		_wmaPeriod = Param(nameof(WmaPeriod), 24)
			.SetDisplay("WT Period", "Period for weighted trend line", "Trigger Line");

		_lsmaPeriod = Param(nameof(LsmaPeriod), 6)
			.SetDisplay("LSMA Period", "Period for least squares moving average", "Trigger Line");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for the strategy", "General");
	}

	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	public int LsmaPeriod
	{
		get => _lsmaPeriod.Value;
		set => _lsmaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_initialized = false;
		_prevLine = 0m;
		_prevSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_wma = new WeightedMovingAverage { Length = WmaPeriod };
		_lsma = new LinearReg { Length = LsmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_wma, _lsma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue, decimal lsmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevLine = wmaValue;
			_prevSignal = lsmaValue;
			_initialized = true;
			return;
		}

		var crossUp = _prevLine <= _prevSignal && wmaValue > lsmaValue;
		var crossDown = _prevLine >= _prevSignal && wmaValue < lsmaValue;

		if (crossUp && Position <= 0)
			BuyMarket();
		else if (crossDown && Position >= 0)
			SellMarket();

		_prevLine = wmaValue;
		_prevSignal = lsmaValue;
	}
}

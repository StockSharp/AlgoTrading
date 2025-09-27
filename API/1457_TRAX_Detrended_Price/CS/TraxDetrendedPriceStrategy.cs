using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TRAX Detrended Price strategy using TRAX and DPO indicators.
/// </summary>
public class TraxDetrendedPriceStrategy : Strategy
{
	private readonly StrategyParam<int> _traxLength;
	private readonly StrategyParam<int> _dpoLength;
	private readonly StrategyParam<int> _confirmLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _sma1 = new();
	private readonly SimpleMovingAverage _sma2 = new();
	private readonly SimpleMovingAverage _sma3 = new();
	private readonly SimpleMovingAverage _ma = new();
	private readonly SimpleMovingAverage _confirm = new();

	private readonly Queue<decimal> _closeQueue = new();

	private decimal _prevSm3;
	private decimal _prevDpo;
	private decimal _prevTrax;
	private bool _isInitialized;

	public TraxDetrendedPriceStrategy()
	{
		_traxLength = Param(nameof(TraxLength), 12)
			.SetDisplay("TRAX Length", "Length for TRAX calculation.", "Indicators");

		_dpoLength = Param(nameof(DpoLength), 19)
			.SetDisplay("DPO Length", "Length for DPO calculation.", "Indicators");

		_confirmLength = Param(nameof(ConfirmLength), 3)
			.SetDisplay("SMA Confirmation Length", "SMA length for trend confirmation.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");
	}

	public int TraxLength
	{
		get => _traxLength.Value;
		set => _traxLength.Value = value;
	}

	public int DpoLength
	{
		get => _dpoLength.Value;
		set => _dpoLength.Value = value;
	}

	public int ConfirmLength
	{
		get => _confirmLength.Value;
		set => _confirmLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_sma1.Reset();
		_sma2.Reset();
		_sma3.Reset();
		_ma.Reset();
		_confirm.Reset();
		_closeQueue.Clear();
		_prevSm3 = 0m;
		_prevDpo = 0m;
		_prevTrax = 0m;
		_isInitialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma1.Length = TraxLength;
		_sma2.Length = TraxLength;
		_sma3.Length = TraxLength;
		_ma.Length = DpoLength;
		_confirm.Length = ConfirmLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var logClose = (decimal)Math.Log((double)candle.ClosePrice);

		var v1 = _sma1.Process(logClose).ToDecimal();
		var v2 = _sma2.Process(v1).ToDecimal();
		var v3 = _sma3.Process(v2).ToDecimal();

		var trax = 10000m * (v3 - _prevSm3);
		_prevSm3 = v3;

		var maValue = _ma.Process(candle.ClosePrice).ToDecimal();

		var barsBack = DpoLength / 2 + 1;
		_closeQueue.Enqueue(candle.ClosePrice);
		if (_closeQueue.Count > barsBack + 1)
			_closeQueue.Dequeue();

		if (_closeQueue.Count <= barsBack)
			return;

		var lagClose = _closeQueue.Peek();
		var dpo = lagClose - maValue;

		var confirm = _confirm.Process(candle.ClosePrice).ToDecimal();

		if (!_isInitialized)
		{
			_prevDpo = dpo;
			_prevTrax = trax;
			_isInitialized = true;
			return;
		}

		var crossOver = _prevDpo <= _prevTrax && dpo > trax;
		var crossUnder = _prevDpo >= _prevTrax && dpo < trax;

		var longCondition = crossOver && trax < 0m && candle.ClosePrice > confirm;
		var shortCondition = crossUnder && trax > 0m && candle.ClosePrice < confirm;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevDpo = dpo;
		_prevTrax = trax;
	}
}

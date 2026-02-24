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
/// Vortex multi-timeframe strategy.
/// Goes long when VI+ crosses above VI- and short on the opposite signal.
/// Manual Vortex calculation with SMA dummy for Bind.
/// </summary>
public class VortexMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _vmPlus = new();
	private readonly List<decimal> _vmMinus = new();
	private readonly List<decimal> _trueRanges = new();
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private decimal _prevVip;
	private decimal _prevVim;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VortexMtfStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period of the Vortex indicator", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_vmPlus.Clear();
		_vmMinus.Clear();
		_trueRanges.Clear();
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;
		_prevVip = 0;
		_prevVim = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_vmPlus.Clear();
		_vmMinus.Clear();
		_trueRanges.Clear();
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;
		_prevVip = 0;
		_prevVim = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHigh == null)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		var vmp = Math.Abs(candle.HighPrice - _prevLow.Value);
		var vmm = Math.Abs(candle.LowPrice - _prevHigh.Value);
		var tr = Math.Max(candle.HighPrice - candle.LowPrice,
			Math.Max(Math.Abs(candle.HighPrice - _prevClose.Value),
				Math.Abs(candle.LowPrice - _prevClose.Value)));

		_vmPlus.Add(vmp);
		_vmMinus.Add(vmm);
		_trueRanges.Add(tr);

		while (_vmPlus.Count > Length)
		{
			_vmPlus.RemoveAt(0);
			_vmMinus.RemoveAt(0);
			_trueRanges.RemoveAt(0);
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;

		if (_vmPlus.Count < Length)
			return;

		var sumTr = _trueRanges.Sum();
		if (sumTr == 0)
			return;

		var vip = _vmPlus.Sum() / sumTr;
		var vim = _vmMinus.Sum() / sumTr;

		if (_prevVip == 0 && _prevVim == 0)
		{
			_prevVip = vip;
			_prevVim = vim;
			return;
		}

		if (_prevVip <= _prevVim && vip > vim && Position <= 0)
			BuyMarket();
		else if (_prevVip >= _prevVim && vip < vim && Position >= 0)
			SellMarket();

		_prevVip = vip;
		_prevVim = vim;
	}
}

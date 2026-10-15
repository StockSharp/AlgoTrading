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
/// Vortex indicator cross with moving average confirmation.
/// Manual Vortex calculation with SMA trend filter.
/// Buys on VI+ crossing above VI- with price above SMA, sells on opposite.
/// </summary>
public class VortexCrossMaConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _vortexLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _vmPlus = new();
	private readonly List<decimal> _vmMinus = new();
	private readonly List<decimal> _trueRanges = new();
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private decimal _prevVip;
	private decimal _prevVim;

	public int VortexLength { get => _vortexLength.Value; set => _vortexLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VortexCrossMaConfirmationStrategy()
	{
		_vortexLength = Param(nameof(VortexLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Vortex indicator period", "General");

		_smaLength = Param(nameof(SmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "MA confirmation period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

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

		var sma = new SimpleMovingAverage { Length = SmaLength };

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
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
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

		// Vortex movement
		var vmp = Math.Abs(candle.HighPrice - _prevLow.Value);
		var vmm = Math.Abs(candle.LowPrice - _prevHigh.Value);
		var tr = Math.Max(candle.HighPrice - candle.LowPrice,
			Math.Max(Math.Abs(candle.HighPrice - _prevClose.Value),
				Math.Abs(candle.LowPrice - _prevClose.Value)));

		_vmPlus.Add(vmp);
		_vmMinus.Add(vmm);
		_trueRanges.Add(tr);

		while (_vmPlus.Count > VortexLength)
		{
			_vmPlus.RemoveAt(0);
			_vmMinus.RemoveAt(0);
			_trueRanges.RemoveAt(0);
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;

		if (_vmPlus.Count < VortexLength)
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

		// Crossover signals with MA confirmation
		var longSignal = _prevVip < _prevVim && vip > vim && candle.ClosePrice > smaVal;
		var shortSignal = _prevVip > _prevVim && vip < vim && candle.ClosePrice < smaVal;

		if (longSignal && Position <= 0)
			BuyMarket();
		else if (shortSignal && Position >= 0)
			SellMarket();

		_prevVip = vip;
		_prevVim = vim;
	}
}

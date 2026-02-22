using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaxPainStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int? _entryBar;
	private readonly List<decimal> _volumes = new();
	private decimal _prevClose;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public int HoldPeriods { get => _holdPeriods.Value; set => _holdPeriods.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaxPainStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20);
		_holdPeriods = Param(nameof(HoldPeriods), 8);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barIndex = 0;
		_entryBar = null;
		_prevClose = 0;
		_volumes.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		_volumes.Add(candle.TotalVolume);
		if (_volumes.Count > LookbackPeriod)
			_volumes.RemoveAt(0);

		if (_volumes.Count < LookbackPeriod || _prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var avgVolume = _volumes.Average();
		var priceChange = Math.Abs(candle.ClosePrice - _prevClose);

		// Volume spike with significant price move
		var painZone = candle.TotalVolume > avgVolume * 1.2m && priceChange > _prevClose * 0.003m;

		if (painZone && Position <= 0)
		{
			BuyMarket();
			_entryBar = _barIndex;
		}

		if (Position > 0 && _entryBar.HasValue && _barIndex >= _entryBar.Value + HoldPeriods)
		{
			SellMarket();
			_entryBar = null;
		}

		_prevClose = candle.ClosePrice;
	}
}

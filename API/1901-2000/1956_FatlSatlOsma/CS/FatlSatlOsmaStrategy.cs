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
/// Simple MACD momentum strategy inspired by FatlSatlOsma.
/// </summary>
public class FatlSatlOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fast;
	private readonly StrategyParam<int> _slow;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1, _prev2;
	private bool _init;
	private int _barsSinceTrade;

	public int Fast { get => _fast.Value; set => _fast.Value = value; }
	public int Slow { get => _slow.Value; set => _slow.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FatlSatlOsmaStrategy()
	{
		_fast = Param(nameof(Fast), 39).SetGreaterThanZero();
		_slow = Param(nameof(Slow), 65).SetGreaterThanZero();
		_buyOpen = Param(nameof(BuyOpen), true);
		_sellOpen = Param(nameof(SellOpen), true);
		_buyClose = Param(nameof(BuyClose), true);
		_sellClose = Param(nameof(SellClose), true);
		_cooldownBars = Param(nameof(CooldownBars), 1);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = _prev2 = 0m;
		_init = false;
		_barsSinceTrade = CooldownBars;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Fast },
				LongMa = { Length = Slow }
			},
			SignalMa = { Length = 9 }
		};
		SubscribeCandles(CandleType)
			.BindEx(macd, OnProcess)
			.Start();
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdNullable = ((MovingAverageConvergenceDivergenceSignalValue)macdVal).Macd;
		if (macdNullable is not decimal val)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		if (!_init)
		{
			_prev1 = _prev2 = val;
			_init = true;
			return;
		}

		if (_prev1 < _prev2)
		{
			if (_barsSinceTrade >= CooldownBars && BuyOpen && val > _prev1 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}
		else if (_prev1 > _prev2)
		{
			if (_barsSinceTrade >= CooldownBars && SellOpen && val < _prev1 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_prev2 = _prev1;
		_prev1 = val;
	}
}

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
/// Triple SMA crossover with stop loss and take profit.
/// </summary>
public class TripleEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _sma1Period;
	private readonly StrategyParam<int> _sma2Period;
	private readonly StrategyParam<int> _sma3Period;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSma1;
	private decimal _prevSma2;
	private decimal _prevSma3;
	private int _cooldown;

	public TripleEmaCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_sma1Period = Param(nameof(Sma1Period), 5)
			.SetDisplay("SMA1 Period", "Period for short SMA", "Indicators");

		_sma2Period = Param(nameof(Sma2Period), 13)
			.SetDisplay("SMA2 Period", "Period for middle SMA", "Indicators");

		_sma3Period = Param(nameof(Sma3Period), 21)
			.SetDisplay("SMA3 Period", "Period for long SMA", "Indicators");

		_stopLossTicks = Param(nameof(StopLossTicks), 200)
			.SetDisplay("Stop Loss (ticks)", "Stop loss in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 200)
			.SetDisplay("Take Profit (ticks)", "Take profit in ticks", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Sma1Period
	{
		get => _sma1Period.Value;
		set => _sma1Period.Value = value;
	}

	public int Sma2Period
	{
		get => _sma2Period.Value;
		set => _sma2Period.Value = value;
	}

	public int Sma3Period
	{
		get => _sma3Period.Value;
		set => _sma3Period.Value = value;
	}

	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
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
		_prevSma1 = 0;
		_prevSma2 = 0;
		_prevSma3 = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma1 = new SimpleMovingAverage { Length = Sma1Period };
		var sma2 = new SimpleMovingAverage { Length = Sma2Period };
		var sma3 = new SimpleMovingAverage { Length = Sma3Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma1, sma2, sma3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma1);
			DrawIndicator(area, sma2);
			DrawIndicator(area, sma3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma1, decimal sma2, decimal sma3)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevSma1 == 0 || _prevSma2 == 0 || _prevSma3 == 0)
		{
			_prevSma1 = sma1;
			_prevSma2 = sma2;
			_prevSma3 = sma3;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevSma1 = sma1;
			_prevSma2 = sma2;
			_prevSma3 = sma3;
			return;
		}

		// SMA1 cross SMA2 — entry signal
		var crossUp = _prevSma1 <= _prevSma2 && sma1 > sma2;
		var crossDown = _prevSma1 >= _prevSma2 && sma1 < sma2;

		// Exit on opposite cross
		if (Position > 0 && crossDown)
		{
			SellMarket();
			_cooldown = 50;
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket();
			_cooldown = 50;
		}

		// Entry on SMA1/SMA2 cross
		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				_cooldown = 50;
			}
			else if (crossDown)
			{
				SellMarket();
				_cooldown = 50;
			}
		}

		_prevSma1 = sma1;
		_prevSma2 = sma2;
		_prevSma3 = sma3;
	}
}

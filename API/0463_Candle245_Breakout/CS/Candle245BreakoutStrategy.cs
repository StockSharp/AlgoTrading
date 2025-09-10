using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Candle245BreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _targetHour;
	private readonly StrategyParam<int> _targetMinute;
	private readonly StrategyParam<int> _lookForwardBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _targetHigh;
	private decimal _targetLow;
	private int _barsLeft;
	private DateTime _lastTargetDay;

	public int TargetHour
	{
		get => _targetHour.Value;
		set => _targetHour.Value = value;
	}

	public int TargetMinute
	{
		get => _targetMinute.Value;
		set => _targetMinute.Value = value;
	}

	public int LookForwardBars
	{
		get => _lookForwardBars.Value;
		set => _lookForwardBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Candle245BreakoutStrategy()
	{
		_targetHour = Param(nameof(TargetHour), 2)
			.SetDisplay("Target Hour", "Hour of the reference candle (UTC)", "General");

		_targetMinute = Param(nameof(TargetMinute), 45)
			.SetDisplay("Target Minute", "Minute of the reference candle (UTC)", "General");

		_lookForwardBars = Param(nameof(LookForwardBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Look Forward Bars", "Number of candles to watch for breakout", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(45).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_targetHigh = default;
		_targetLow = default;
		_barsLeft = default;
		_lastTargetDay = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.OpenTime.Date;

		if (date != _lastTargetDay && candle.OpenTime.Hour == TargetHour && candle.OpenTime.Minute == TargetMinute)
		{
			_targetHigh = candle.HighPrice;
			_targetLow = candle.LowPrice;
			_barsLeft = LookForwardBars;
			_lastTargetDay = date;
			LogInfo($"Target candle captured. High={_targetHigh}, Low={_targetLow}");
			return;
		}

		if (_barsLeft <= 0)
			return;

		_barsLeft--;

		if (candle.HighPrice > _targetHigh && Position <= 0)
		{
			RegisterBuy(Volume + Math.Abs(Position));
			LogInfo("Breakout above target high. Buying.");
		}
		else if (candle.LowPrice < _targetLow && Position >= 0)
		{
			RegisterSell(Volume + Math.Abs(Position));
			LogInfo("Breakout below target low. Selling.");
		}

		if (_barsLeft == 0 && Position != 0)
		{
			if (Position > 0)
			RegisterSell(Position);
			else
			RegisterBuy(Math.Abs(Position));

			LogInfo("Closing position at end of breakout window.");
		}
	}
}

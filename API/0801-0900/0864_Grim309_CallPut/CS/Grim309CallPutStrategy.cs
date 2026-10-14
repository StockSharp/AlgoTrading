using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GRIM309 CallPut strategy.
/// Goes long when EMA10 is above EMA20, price is above EMA50 and a rising EMA5 is above EMA10; goes short on the mirrored
/// conditions. Entries need a flat position and at least CooldownBars candles since the last exit. A position closes when price
/// crosses EMA15 against it or when the warning fires: the EMA5-EMA10 spread in the trade's direction shrinks to less than half
/// of its previous value.
/// </summary>
public class Grim309CallPutStrategy : Strategy
{
	private readonly StrategyParam<int> _ema5Length;
	private readonly StrategyParam<int> _ema10Length;
	private readonly StrategyParam<int> _ema15Length;
	private readonly StrategyParam<int> _ema20Length;
	private readonly StrategyParam<int> _ema50Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevEma5;
	private decimal? _prevSpread;
	private int _barsSinceExit;

	/// <summary>
	/// Length of the fastest EMA.
	/// </summary>
	public int Ema5Length
	{
		get => _ema5Length.Value;
		set => _ema5Length.Value = value;
	}

	/// <summary>
	/// Length of the EMA paired with EMA5.
	/// </summary>
	public int Ema10Length
	{
		get => _ema10Length.Value;
		set => _ema10Length.Value = value;
	}

	/// <summary>
	/// Length of the exit EMA.
	/// </summary>
	public int Ema15Length
	{
		get => _ema15Length.Value;
		set => _ema15Length.Value = value;
	}

	/// <summary>
	/// Length of the EMA compared with EMA10.
	/// </summary>
	public int Ema20Length
	{
		get => _ema20Length.Value;
		set => _ema20Length.Value = value;
	}

	/// <summary>
	/// Length of the trend EMA price is compared with.
	/// </summary>
	public int Ema50Length
	{
		get => _ema50Length.Value;
		set => _ema50Length.Value = value;
	}

	/// <summary>
	/// Length of the long-term EMA shown on the chart.
	/// </summary>
	public int Ema200Length
	{
		get => _ema200Length.Value;
		set => _ema200Length.Value = value;
	}

	/// <summary>
	/// Candles to wait after an exit before a new entry.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Grim309CallPutStrategy()
	{
		_ema5Length = Param(nameof(Ema5Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA5 Length", "Length of the fastest EMA", "Indicators");

		_ema10Length = Param(nameof(Ema10Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA10 Length", "Length of the EMA paired with EMA5", "Indicators");

		_ema15Length = Param(nameof(Ema15Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("EMA15 Length", "Length of the exit EMA", "Indicators");

		_ema20Length = Param(nameof(Ema20Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA20 Length", "Length of the EMA compared with EMA10", "Indicators");

		_ema50Length = Param(nameof(Ema50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA50 Length", "Length of the trend EMA", "Indicators");

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA200 Length", "Length of the long-term EMA", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Candles to wait after an exit before a new entry", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevEma5 = null;
		_prevSpread = null;
		_barsSinceExit = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema5 = new ExponentialMovingAverage { Length = Ema5Length };
		var ema10 = new ExponentialMovingAverage { Length = Ema10Length };
		var ema15 = new ExponentialMovingAverage { Length = Ema15Length };
		var ema20 = new ExponentialMovingAverage { Length = Ema20Length };
		var ema50 = new ExponentialMovingAverage { Length = Ema50Length };
		var ema200 = new ExponentialMovingAverage { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema5, ema10, ema15, ema20, ema50, ema200, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema5);
			DrawIndicator(area, ema10);
			DrawIndicator(area, ema15);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawIndicator(area, ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema10, decimal ema15, decimal ema20, decimal ema50, decimal ema200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_barsSinceExit != int.MaxValue)
			_barsSinceExit++;

		var prevEma5 = _prevEma5;
		var prevSpread = _prevSpread;
		var spread = ema5 - ema10;

		_prevEma5 = ema5;
		_prevSpread = spread;

		if (prevEma5 is not decimal lastEma5 || prevSpread is not decimal lastSpread)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			var warning = lastSpread > 0 && spread < lastSpread / 2;

			if (close < ema15 || warning)
			{
				SellMarket(Position);
				_barsSinceExit = 0;
			}

			return;
		}

		if (Position < 0)
		{
			var warning = lastSpread < 0 && spread > lastSpread / 2;

			if (close > ema15 || warning)
			{
				BuyMarket(-Position);
				_barsSinceExit = 0;
			}

			return;
		}

		if (_barsSinceExit < CooldownBars)
			return;

		if (ema10 > ema20 && close > ema50 && ema5 > ema10 && ema5 > lastEma5)
			BuyMarket(Volume);
		else if (ema10 < ema20 && close < ema50 && ema5 < ema10 && ema5 < lastEma5)
			SellMarket(Volume);
	}
}

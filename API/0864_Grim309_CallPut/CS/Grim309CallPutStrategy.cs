using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA alignment with warning system and cooldown.
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

	private EMA _ema5;
	private EMA _ema10;
	private EMA _ema15;
	private EMA _ema20;
	private EMA _ema50;
	private EMA _ema200;

	private decimal _prevEma5;
	private int _barsSinceClose;
	private decimal _diff1;
	private decimal _diff2;
	private decimal _diff3;
	private decimal _diff4;
	private decimal _diff5;
	private int _diffCount;

	/// <summary>
	/// EMA 5 period length.
	/// </summary>
	public int Ema5Length { get => _ema5Length.Value; set => _ema5Length.Value = value; }

	/// <summary>
	/// EMA 10 period length.
	/// </summary>
	public int Ema10Length { get => _ema10Length.Value; set => _ema10Length.Value = value; }

	/// <summary>
	/// EMA 15 period length.
	/// </summary>
	public int Ema15Length { get => _ema15Length.Value; set => _ema15Length.Value = value; }

	/// <summary>
	/// EMA 20 period length.
	/// </summary>
	public int Ema20Length { get => _ema20Length.Value; set => _ema20Length.Value = value; }

	/// <summary>
	/// EMA 50 period length.
	/// </summary>
	public int Ema50Length { get => _ema50Length.Value; set => _ema50Length.Value = value; }

	/// <summary>
	/// EMA 200 period length.
	/// </summary>
	public int Ema200Length { get => _ema200Length.Value; set => _ema200Length.Value = value; }

	/// <summary>
	/// Cooldown period in bars after closing position.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Grim309CallPutStrategy"/> class.
	/// </summary>
	public Grim309CallPutStrategy()
	{
		_ema5Length = Param(nameof(Ema5Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA5 Length", "Period of EMA 5", "EMAs");

		_ema10Length = Param(nameof(Ema10Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA10 Length", "Period of EMA 10", "EMAs");

		_ema15Length = Param(nameof(Ema15Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("EMA15 Length", "Period of EMA 15", "EMAs");

		_ema20Length = Param(nameof(Ema20Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA20 Length", "Period of EMA 20", "EMAs");

		_ema50Length = Param(nameof(Ema50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA50 Length", "Period of EMA 50", "EMAs");

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA200 Length", "Period of EMA 200", "EMAs");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars after close before new entry", "General");

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

		_prevEma5 = 0m;
		_barsSinceClose = int.MaxValue;
		_diff1 = 0m;
		_diff2 = 0m;
		_diff3 = 0m;
		_diff4 = 0m;
		_diff5 = 0m;
		_diffCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ema5 = new EMA { Length = Ema5Length };
		_ema10 = new EMA { Length = Ema10Length };
		_ema15 = new EMA { Length = Ema15Length };
		_ema20 = new EMA { Length = Ema20Length };
		_ema50 = new EMA { Length = Ema50Length };
		_ema200 = new EMA { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema5, _ema10, _ema15, _ema20, _ema50, _ema200, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema5);
			DrawIndicator(area, _ema10);
			DrawIndicator(area, _ema15);
			DrawIndicator(area, _ema20);
			DrawIndicator(area, _ema50);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema10, decimal ema15, decimal ema20, decimal ema50, decimal ema200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!(_ema5.IsFormed && _ema10.IsFormed && _ema15.IsFormed && _ema20.IsFormed && _ema50.IsFormed && _ema200.IsFormed))
			return;

		var diffNow = Math.Abs((ema5 - ema10) / ema10 * 100m);

		var avgChange = 0m;
		var isWarning = false;

		if (_diffCount >= 5)
		{
			avgChange = (Math.Abs(_diff1 - _diff2) + Math.Abs(_diff2 - _diff3) + Math.Abs(_diff3 - _diff4) + Math.Abs(_diff4 - _diff5)) / 4m;
			isWarning = Position != 0 && Math.Abs(diffNow - _diff1) > 2.5m * avgChange && diffNow < _diff1;
		}

		var uptrend = ema10 > ema20 && candle.ClosePrice > ema50;
		var downtrend = ema10 < ema20 && candle.ClosePrice < ema50;

		var emaCheckCall = ema5 > _prevEma5 && ema5 > ema10;
		var emaCheckPut = ema5 < _prevEma5 && ema5 < ema10;

		var canOpen = _barsSinceClose == int.MaxValue || _barsSinceClose >= CooldownBars;

		if (Position == 0 && canOpen)
		{
			if (uptrend && emaCheckCall)
			{
				BuyMarket(Volume);
				_barsSinceClose = int.MaxValue;
			}
			else if (downtrend && emaCheckPut)
			{
				SellMarket(Volume);
				_barsSinceClose = int.MaxValue;
			}
		}
		else if (Position > 0 && (candle.ClosePrice <= ema15 || isWarning))
		{
			SellMarket(Math.Abs(Position));
			_barsSinceClose = 0;
		}
		else if (Position < 0 && (candle.ClosePrice >= ema15 || isWarning))
		{
			BuyMarket(Math.Abs(Position));
			_barsSinceClose = 0;
		}

		if (_barsSinceClose != int.MaxValue && Position == 0)
			_barsSinceClose++;

		_prevEma5 = ema5;

		ShiftDiffs(diffNow);
		if (_diffCount < 5)
			_diffCount++;
	}

	private void ShiftDiffs(decimal diffNow)
	{
		_diff5 = _diff4;
		_diff4 = _diff3;
		_diff3 = _diff2;
		_diff2 = _diff1;
		_diff1 = diffNow;
	}
}


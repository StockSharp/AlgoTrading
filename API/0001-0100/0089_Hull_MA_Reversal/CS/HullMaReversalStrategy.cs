using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull MA Reversal strategy.
/// Enters long when Hull MA changes direction from down to up.
/// Enters short when Hull MA changes direction from up to down.
/// Uses cooldown to control trade frequency.
/// </summary>
public class HullMaReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevHma;
	private decimal _prevPrevHma;
	private int _cooldown;

	/// <summary>
	/// HMA period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HullMaReversalStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetRange(5, 20)
			.SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevHma = default;
		_prevPrevHma = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHma = 0;
		_prevPrevHma = 0;
		_cooldown = 0;

		var hma = new HullMovingAverage { Length = HmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevHma == 0)
		{
			_prevHma = hmaValue;
			return;
		}

		if (_prevPrevHma == 0)
		{
			_prevPrevHma = _prevHma;
			_prevHma = hmaValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevHma = _prevHma;
			_prevHma = hmaValue;
			return;
		}

		// Direction change detection
		var dirChangedUp = _prevHma < _prevPrevHma && hmaValue > _prevHma;
		var dirChangedDown = _prevHma > _prevPrevHma && hmaValue < _prevHma;

		if (Position == 0 && dirChangedUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && dirChangedDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && dirChangedDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && dirChangedUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrevHma = _prevHma;
		_prevHma = hmaValue;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Hook Reversal strategy.
/// Enters long when %K hooks up from oversold zone.
/// Enters short when %K hooks down from overbought zone.
/// Exits when %K reaches neutral zone.
/// Uses cooldown to control trade frequency.
/// </summary>
public class StochasticHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<int> _exitLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevK;
	private int _cooldown;

	/// <summary>
	/// %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Exit level (neutral zone).
	/// </summary>
	public int ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
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
	public StochasticHookReversalStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("K Period", "%K period", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetRange(1, 5)
			.SetDisplay("D Period", "%D period", "Stochastic");

		_oversoldLevel = Param(nameof(OversoldLevel), 20)
			.SetRange(10, 30)
			.SetDisplay("Oversold", "Oversold level", "Stochastic");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80)
			.SetRange(70, 90)
			.SetDisplay("Overbought", "Overbought level", "Stochastic");

		_exitLevel = Param(nameof(ExitLevel), 50)
			.SetRange(45, 55)
			.SetDisplay("Exit Level", "Neutral exit zone", "Stochastic");

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
		_prevK = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_cooldown = 0;

		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochIv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochIv.IsFormed)
			return;

		var sv = (IStochasticOscillatorValue)stochIv;

		if (sv.K is not decimal kValue)
			return;

		if (_prevK == null)
		{
			_prevK = kValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevK = kValue;
			return;
		}

		// Hook up from oversold
		var oversoldHookUp = _prevK < OversoldLevel && kValue > _prevK;
		// Hook down from overbought
		var overboughtHookDown = _prevK > OverboughtLevel && kValue < _prevK;

		if (Position == 0 && oversoldHookUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && overboughtHookDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && kValue < ExitLevel)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && kValue > ExitLevel)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevK = kValue;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on Stochastic Oscillator Failure Swing pattern.
/// A failure swing occurs when Stochastic reverses direction without crossing through centerline.
/// Uses cooldown to control trade frequency.
/// </summary>
public class StochasticFailureSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private StochasticOscillator _stochastic;

	private decimal _prevK;
	private decimal _prevPrevK;
	private int _cooldown;

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public StochasticFailureSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetDisplay("K Period", "%K period", "Stochastic")
			.SetRange(5, 30);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("D Period", "%D period", "Stochastic")
			.SetRange(2, 10);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Stochastic oversold", "Stochastic")
			.SetRange(10m, 40m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Stochastic overbought", "Stochastic")
			.SetRange(60m, 90m);

		_cooldownBars = Param(nameof(CooldownBars), 250)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 2000);
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
		_stochastic = default;
		_prevK = 0;
		_prevPrevK = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal kValue)
			return;

		// Need at least 2 previous values
		if (_prevK == 0 || _prevPrevK == 0)
		{
			_prevPrevK = _prevK;
			_prevK = kValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevK = _prevK;
			_prevK = kValue;
			return;
		}

		// Bullish Failure Swing: K was oversold, rose, pulled back but stayed above prior low
		var isBullish = _prevPrevK < OversoldLevel &&
			_prevK > _prevPrevK &&
			kValue < _prevK &&
			kValue > _prevPrevK;

		// Bearish Failure Swing: K was overbought, fell, bounced but stayed below prior high
		var isBearish = _prevPrevK > OverboughtLevel &&
			_prevK < _prevPrevK &&
			kValue > _prevK &&
			kValue < _prevPrevK;

		if (Position == 0)
		{
			if (isBullish)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isBearish)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			// Exit long when K crosses above overbought
			if (kValue > OverboughtLevel)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short when K crosses below oversold
			if (kValue < OversoldLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevPrevK = _prevK;
		_prevK = kValue;
	}
}

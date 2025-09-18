using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on MACD signal line crossing the zero level.
/// </summary>
public class MacdWaterlineCrossExpectatorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<RiskBenefitRatio> _riskBenefit;
	private readonly StrategyParam<DataType> _candleType;

	private bool _shouldBuy;
	private bool _hasPrev;
	private decimal _prevSignal;
	private decimal _takeProfit;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio preset.
	/// </summary>
	public RiskBenefitRatio RiskBenefit
	{
		get => _riskBenefit.Value;
		set => _riskBenefit.Value = value;
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
	/// Available risk to reward ratios.
	/// </summary>
	public enum RiskBenefitRatio
	{
		OneFive,
		OneFour,
		OneThree,
		OneTwo,
		One
	}

	/// <summary>
	/// Initializes <see cref="MacdWaterlineCrossExpectatorStrategy"/>.
	/// </summary>
	public MacdWaterlineCrossExpectatorStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal", "Signal line period", "Indicators");
		_stopLoss = Param(nameof(StopLoss), 0.003m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order volume", "Trading");
		_riskBenefit = Param(nameof(RiskBenefit), RiskBenefitRatio.OneTwo)
			.SetDisplay("RR", "Risk benefit ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle", "Candle time frame", "General");
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
		_shouldBuy = true;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var multiplier = RiskBenefit switch
		{
			RiskBenefitRatio.OneFive => 5m,
			RiskBenefitRatio.OneFour => 4m,
			RiskBenefitRatio.OneThree => 3m,
			RiskBenefitRatio.OneTwo => 2m,
			_ => 1m
		};
		_takeProfit = StopLoss * multiplier;

		StartProtection(
			takeProfit: new Unit(_takeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute)
		);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var signal = typed.Signal;

		if (!_hasPrev)
		{
			_prevSignal = signal;
			_hasPrev = true;
			return;
		}

		var crossedAbove = _prevSignal < 0 && signal > 0;
		var crossedBelow = _prevSignal > 0 && signal < 0;

		if (crossedAbove && _shouldBuy)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_shouldBuy = false;
			LogInfo($"Buy signal at {signal:F5}");
		}
		else if (crossedBelow && !_shouldBuy)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shouldBuy = true;
			LogInfo($"Sell signal at {signal:F5}");
		}

		_prevSignal = signal;
	}
}

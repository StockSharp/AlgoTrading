using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades using a selectable chart oscillator (Stochastic, RSI, or MFI).
/// </summary>
public class ChartOscillatorStrategy : Strategy
{
	public enum OscillatorChoice
	{
		Stochastic,
		Rsi,
		Mfi
	}

	private readonly StrategyParam<OscillatorChoice> _choice;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private StochasticOscillator _stochastic;
	private RelativeStrengthIndex _rsi;
	private MoneyFlowIndex _mfi;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;

	/// <summary>
	/// Selected oscillator type.
	/// </summary>
	public OscillatorChoice Choice { get => _choice.Value; set => _choice.Value = value; }

	/// <summary>
	/// Period for RSI or MFI.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Base period for Stochastic oscillator.
	/// </summary>
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }

	/// <summary>
	/// Smoothing period for %D.
	/// </summary>
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }

	/// <summary>
	/// Smoothing period for %K.
	/// </summary>
	public int SmoothK { get => _smoothK.Value; set => _smoothK.Value = value; }

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Stop-loss percentage from entry.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChartOscillatorStrategy"/>.
	/// </summary>
	public ChartOscillatorStrategy()
	{
		_choice = Param(nameof(Choice), OscillatorChoice.Stochastic)
			.SetDisplay("Oscillator", "Type of oscillator to use", "General");

		_length = Param(nameof(Length), 14)
			.SetRange(1, 100)
			.SetDisplay("RSI/MFI Length", "Period for RSI or MFI", "Indicator Parameters")
			.SetCanOptimize(true);

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetRange(1, 100)
			.SetDisplay("Stochastic Length", "Base period for Stochastic", "Indicator Parameters")
			.SetCanOptimize(true);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetRange(1, 50)
			.SetDisplay("D Period", "Smoothing period for %D", "Indicator Parameters")
			.SetCanOptimize(true);

		_smoothK = Param(nameof(SmoothK), 3)
			.SetRange(1, 50)
			.SetDisplay("Smooth K", "Smoothing period for %K", "Indicator Parameters")
			.SetCanOptimize(true);

		_overbought = Param(nameof(Overbought), 80m)
			.SetRange(50m, 100m)
			.SetDisplay("Overbought", "Level considered overbought", "Signal Parameters")
			.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 20m)
			.SetRange(0m, 50m)
			.SetDisplay("Oversold", "Level considered oversold", "Signal Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Percentage-based stop loss", "Risk Management")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Choice == OscillatorChoice.Stochastic)
		{
			_stochastic = new StochasticOscillator
			{
				Length = KPeriod,
				K = { Length = SmoothK },
				D = { Length = DPeriod }
			};
		}
		else if (Choice == OscillatorChoice.Rsi)
		{
			_rsi = new RelativeStrengthIndex { Length = Length };
		}
		else
		{
			_mfi = new MoneyFlowIndex { Length = Length };
		}

		var subscription = SubscribeCandles(CandleType);

		if (Choice == OscillatorChoice.Stochastic)
			subscription.BindEx(_stochastic, ProcessStochastic).Start();
		else if (Choice == OscillatorChoice.Rsi)
			subscription.Bind(_rsi, ProcessRsi).Start();
		else
			subscription.Bind(_mfi, ProcessMfi).Start();

		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (Choice == OscillatorChoice.Stochastic)
				DrawIndicator(area, _stochastic);
			else if (Choice == OscillatorChoice.Rsi)
				DrawIndicator(area, _rsi);
			else
				DrawIndicator(area, _mfi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;

		var k = stoch.K;
		var d = stoch.D;

		if (_hasPrev)
		{
			var crossUp = _prevK <= _prevD && k > d && k <= Oversold;
			var crossDown = _prevK >= _prevD && k < d && k >= Overbought;

			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (Position > 0 && k >= 50m)
			SellMarket(Position);
		else if (Position < 0 && k <= 50m)
			BuyMarket(Math.Abs(Position));

		_prevK = k;
		_prevD = d;
		_hasPrev = true;
	}

	private void ProcessRsi(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsiValue <= Oversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (rsiValue >= Overbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && rsiValue >= 50m)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && rsiValue <= 50m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void ProcessMfi(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (mfiValue <= Oversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (mfiValue >= Overbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && mfiValue >= 50m)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && mfiValue <= 50m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}


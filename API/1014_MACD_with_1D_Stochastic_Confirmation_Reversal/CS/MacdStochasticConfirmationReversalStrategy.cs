using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that goes long on MACD crossover confirmed by daily Stochastic.
/// Uses ATR based stop loss and trailing EMA take profit.
/// </summary>
public class MacdStochasticConfirmationReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _trailingEmaLength;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<decimal> _trailingActivationAtr;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;
	private StochasticOscillator _dailyStoch;

	private decimal? _dailyK;
	private decimal? _dailyD;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrev;

	private decimal _stopLossLevel;
	private decimal _activationLevel;
	private bool _trailingActive;
	private decimal? _takeProfitLevel;
	private decimal _entryPrice;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Trailing EMA length.
	/// </summary>
	public int TrailingEmaLength { get => _trailingEmaLength.Value; set => _trailingEmaLength.Value = value; }

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopLossAtrMultiplier { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }

	/// <summary>
	/// ATR multiplier to activate trailing.
	/// </summary>
	public decimal TrailingActivationAtrMultiplier { get => _trailingActivationAtr.Value; set => _trailingActivationAtr.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MacdStochasticConfirmationReversalStrategy()
	{
		_macdFast = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast EMA length", "MACD");

		_macdSlow = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow EMA length", "MACD");

		_macdSignal = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal EMA length", "MACD");

		_trailingEmaLength = Param(nameof(TrailingEmaLength), 20)
			.SetDisplay("Trailing EMA", "EMA length for trailing take profit", "Strategy");

		_stopLossAtr = Param(nameof(StopLossAtrMultiplier), 3.25m)
			.SetDisplay("ATR Stop", "ATR multiplier for stop loss", "Strategy");

		_trailingActivationAtr = Param(nameof(TrailingActivationAtrMultiplier), 4.25m)
			.SetDisplay("ATR Activate", "ATR multiplier to activate trailing", "Strategy");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle Type", "Base candle type", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};

		_atr = new AverageTrueRange { Length = 14 };
		_ema = new ExponentialMovingAverage { Length = TrailingEmaLength };
		_dailyStoch = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _atr, _ema, ProcessCandle)
			.Start();

		var dailySubscription = SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1)));
		dailySubscription
			.BindEx(_dailyStoch, ProcessDaily)
			.Start();
	}

	private void ProcessDaily(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is decimal k && stoch.D is decimal d)
		{
			_dailyK = k;
			_dailyD = d;
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			return;

		var atr = atrValue.ToDecimal();
		var ema = emaValue.ToDecimal();

		if (!_macd.IsFormed || _dailyK is null || _dailyD is null)
		{
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			_hasPrev = true;
			return;
		}

		var crossUp = _hasPrev && _prevMacd <= _prevSignal && macdLine > signalLine;

		if (crossUp && _dailyK > _dailyD && _dailyK < 80m && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopLossLevel = _entryPrice - StopLossAtrMultiplier * atr;
			_activationLevel = _entryPrice + TrailingActivationAtrMultiplier * atr;
			_trailingActive = false;
			_takeProfitLevel = null;
		}

		if (Position > 0)
		{
			if (!_trailingActive && candle.HighPrice > _activationLevel)
				_trailingActive = true;

			if (_trailingActive)
				_takeProfitLevel = ema;

			if ((_takeProfitLevel is decimal tp && candle.ClosePrice < tp) ||
				candle.LowPrice <= _stopLossLevel)
			{
				SellMarket(Position);
				_trailingActive = false;
				_takeProfitLevel = null;
			}
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
		_hasPrev = true;
	}
}


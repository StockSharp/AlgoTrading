using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the "3Commas Bot" TradingView strategy.
/// Uses two moving averages for trend detection and manages exits
/// through configurable risk to reward and optional ATR trailing stop.
/// </summary>
public class ThreeCommasBotStrategy : Strategy
{
	private readonly StrategyParam<bool> _longTrades;
	private readonly StrategyParam<bool> _shortTrades;
	private readonly StrategyParam<bool> _useLimit;
	private readonly StrategyParam<bool> _trailStop;
	private readonly StrategyParam<bool> _flip;
	private readonly StrategyParam<decimal> _rnR;
	private readonly StrategyParam<decimal> _riskM;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailStopSize;
	private readonly StrategyParam<decimal> _rrExit;
	private readonly StrategyParam<int> _maLength1;
	private readonly StrategyParam<int> _maLength2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _tradeStopPrice;
	private decimal _tradeTargetPrice;
	private decimal _tradeExitTriggerPrice;
	private decimal _trailingStop;
	private bool _lookForExit;
	private bool _initialized;
	private bool _wasFastAboveSlow;

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool LongTrades { get => _longTrades.Value; set => _longTrades.Value = value; }

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool ShortTrades { get => _shortTrades.Value; set => _shortTrades.Value = value; }

	/// <summary>
	/// Use fixed target price for exits.
	/// </summary>
	public bool UseLimit { get => _useLimit.Value; set => _useLimit.Value = value; }

	/// <summary>
	/// Use ATR based trailing stop.
	/// </summary>
	public bool TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }

	/// <summary>
	/// Allow reversing position on opposite signal.
	/// </summary>
	public bool Flip { get => _flip.Value; set => _flip.Value = value; }

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RnR { get => _rnR.Value; set => _rnR.Value = value; }

	/// <summary>
	/// Risk multiplier for ATR based stop.
	/// </summary>
	public decimal RiskM { get => _riskM.Value; set => _riskM.Value = value; }

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal TrailStopSize { get => _trailStopSize.Value; set => _trailStopSize.Value = value; }

	/// <summary>
	/// Fraction of reward to trigger trailing stop.
	/// </summary>
	public decimal RrExit { get => _rrExit.Value; set => _rrExit.Value = value; }

	/// <summary>
	/// First MA length.
	/// </summary>
	public int MaLength1 { get => _maLength1.Value; set => _maLength1.Value = value; }

	/// <summary>
	/// Second MA length.
	/// </summary>
	public int MaLength2 { get => _maLength2.Value; set => _maLength2.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ThreeCommasBotStrategy()
	{
		_longTrades = Param(nameof(LongTrades), true)
			.SetDisplay("Detect Long Trades", "Enable long entries", "Trade variables");

		_shortTrades = Param(nameof(ShortTrades), true)
			.SetDisplay("Detect Short Trades", "Enable short entries", "Trade variables");

		_useLimit = Param(nameof(UseLimit), true)
			.SetDisplay("Use Limit exit", "Use fixed reward target", "Trade variables");

		_trailStop = Param(nameof(TrailStop), false)
			.SetDisplay("Use ATR Trailing Stop", "Enable ATR trailing stop", "Trade variables");

		_flip = Param(nameof(Flip), false)
			.SetDisplay("Allow Reversal Trades", "Flip position on opposite signal", "Trade variables");

		_rnR = Param(nameof(RnR), 1m)
			.SetDisplay("Reward to Risk Ratio", "Target distance as a multiple of risk", "Risk Management")
			.SetGreaterThanZero();

		_riskM = Param(nameof(RiskM), 1m)
			.SetDisplay("Risk Adjustment", "ATR multiplier for stop", "Risk Management")
			.SetGreaterThanZero();

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR length", "ATR calculation period", "Risk Management")
			.SetGreaterThanZero();

		_trailStopSize = Param(nameof(TrailStopSize), 1m)
			.SetDisplay("ATR Trailing Stop Multiplier", "ATR multiplier for trailing", "Trailing Stop")
			.SetGreaterThanZero();

		_rrExit = Param(nameof(RrExit), 0m)
			.SetDisplay("R:R To Trigger Exit", "Fraction of reward before trailing", "Trailing Stop");

		_maLength1 = Param(nameof(MaLength1), 21)
			.SetDisplay("MA Length #1", "Fast moving average length", "MA Settings")
			.SetGreaterThanZero();

		_maLength2 = Param(nameof(MaLength2), 50)
			.SetDisplay("MA Length #2", "Slow moving average length", "MA Settings")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_tradeStopPrice = 0m;
		_tradeTargetPrice = 0m;
		_tradeExitTriggerPrice = 0m;
		_trailingStop = 0m;
		_lookForExit = false;
		_initialized = false;
		_wasFastAboveSlow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var fastMa = new EMA { Length = MaLength1 };
		var slowMa = new EMA { Length = MaLength2 };
		var atr = new ATR { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_wasFastAboveSlow = fastValue > slowValue;
			_initialized = true;
			return;
		}

		var isFastAboveSlow = fastValue > slowValue;

		if (_wasFastAboveSlow != isFastAboveSlow)
		{
			if (isFastAboveSlow)
				TryEnterLong(candle, atrValue);
			else
				TryEnterShort(candle, atrValue);

			_wasFastAboveSlow = isFastAboveSlow;
		}

		ManagePosition(candle, atrValue);
	}

	private void TryEnterLong(ICandleMessage candle, decimal atrValue)
	{
		if (!LongTrades)
			return;

		if (Position > 0)
			return;

		var volume = Volume;
		if (Position < 0)
		{
			if (!Flip)
				return;

			volume += Math.Abs(Position);
		}

		var entry = candle.ClosePrice;
		var stop = entry - atrValue * RiskM;
		var risk = entry - stop;
		var target = UseLimit ? entry + risk * RnR : 0m;
		var trigger = entry + risk * RnR * RrExit;

		_tradeStopPrice = stop;
		_tradeTargetPrice = target;
		_tradeExitTriggerPrice = trigger;
		_trailingStop = stop;
		_lookForExit = false;

		BuyMarket(volume);
	}

	private void TryEnterShort(ICandleMessage candle, decimal atrValue)
	{
		if (!ShortTrades)
			return;

		if (Position < 0)
			return;

		var volume = Volume;
		if (Position > 0)
		{
			if (!Flip)
				return;

			volume += Math.Abs(Position);
		}

		var entry = candle.ClosePrice;
		var stop = entry + atrValue * RiskM;
		var risk = stop - entry;
		var target = UseLimit ? entry - risk * RnR : 0m;
		var trigger = entry - risk * RnR * RrExit;

		_tradeStopPrice = stop;
		_tradeTargetPrice = target;
		_tradeExitTriggerPrice = trigger;
		_trailingStop = stop;
		_lookForExit = false;

		SellMarket(volume);
	}

	private void ManagePosition(ICandleMessage candle, decimal atrValue)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _tradeStopPrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (UseLimit && _tradeTargetPrice > 0m && candle.HighPrice >= _tradeTargetPrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (TrailStop)
			{
				if (!_lookForExit)
				{
					if (RrExit == 0m || candle.HighPrice >= _tradeExitTriggerPrice)
						_lookForExit = true;
				}

				if (_lookForExit)
				{
					var candidate = candle.ClosePrice - atrValue * TrailStopSize;
					if (candidate > _trailingStop)
						_trailingStop = candidate;

					if (candle.ClosePrice <= _trailingStop)
						SellMarket(Math.Abs(Position));
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _tradeStopPrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (UseLimit && _tradeTargetPrice > 0m && candle.LowPrice <= _tradeTargetPrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (TrailStop)
			{
				if (!_lookForExit)
				{
					if (RrExit == 0m || candle.LowPrice <= _tradeExitTriggerPrice)
						_lookForExit = true;
				}

				if (_lookForExit)
				{
					var candidate = candle.ClosePrice + atrValue * TrailStopSize;
					if (candidate < _trailingStop)
						_trailingStop = candidate;

					if (candle.ClosePrice >= _trailingStop)
						BuyMarket(Math.Abs(Position));
				}
			}
		}
	}
}

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zero-lag volatility breakout strategy with EMA trend filter.
/// </summary>
public class ZeroLagVolatilityBreakoutEmaTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<bool> _useBinary;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private BollingerBands _bollinger = null!;

	private decimal _prevDif;
	private decimal _prevBbu;
	private decimal _prevBbm;
	private decimal _prevEma;

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal StdMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Use binary strategy (hold until opposite signal).
	/// </summary>
	public bool UseBinary
	{
		get => _useBinary.Value;
		set => _useBinary.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagVolatilityBreakoutEmaTrendStrategy"/>.
	/// </summary>
	public ZeroLagVolatilityBreakoutEmaTrendStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200).SetDisplay("EMA Length", "Base EMA length", "Indicators");
		_stdMultiplier = Param(nameof(StdMultiplier), 2m).SetDisplay("Std Mult", "Standard deviation multiplier", "Indicators");
		_useBinary = Param(nameof(UseBinary), true).SetDisplay("Use Binary", "Hold until opposite signal", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_bollinger = new BollingerBands { Length = EmaLength, Width = StdMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hJumper = Math.Max(candle.ClosePrice, emaValue);
		var lJumper = Math.Min(candle.ClosePrice, emaValue);
		var dif = lJumper == 0 ? 0 : (hJumper / lJumper) - 1m;

		var bbVal = (BollingerBandsValue)_bollinger.Process(dif, candle.CloseTime, true);
		if (bbVal.UpBand is not decimal bbu || bbVal.MovingAverage is not decimal bbm)
			return;

		var sigEnter = _prevDif <= _prevBbu && dif > bbu;
		var sigExit = _prevDif >= _prevBbm && dif < bbm;
		var enterLong = sigEnter && emaValue > _prevEma;
		var enterShort = sigEnter && emaValue < _prevEma;

		if (enterLong && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (enterShort && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (!UseBinary && sigExit)
		{
			if (Position > 0)
			    SellMarket(Position);
			else if (Position < 0)
			    BuyMarket(Math.Abs(Position));
		}

		_prevDif = dif;
		_prevBbu = bbu;
		_prevBbm = bbm;
		_prevEma = emaValue;
	}
}

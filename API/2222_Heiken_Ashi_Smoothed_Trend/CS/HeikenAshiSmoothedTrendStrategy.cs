namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Heiken-Ashi strategy using EMA-smoothed candles.
/// Opens long positions when candle color turns bullish and short positions when it turns bearish.
/// </summary>
public class HeikenAshiSmoothedTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _openEma;
	private ExponentialMovingAverage _closeEma;
	private ExponentialMovingAverage _highEma;
	private ExponentialMovingAverage _lowEma;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;
	private bool? _prevIsGreen;

	/// <summary>
	/// Length for EMA smoothing.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HeikenAshiSmoothedTrendStrategy"/>.
	/// </summary>
	public HeikenAshiSmoothedTrendStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 30)
			.SetDisplay("EMA Length", "Length for smoothing", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_openEma = default;
		_closeEma = default;
		_highEma = default;
		_lowEma = default;
		_prevHaOpen = null;
		_prevHaClose = null;
		_prevIsGreen = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openEma = new ExponentialMovingAverage { Length = EmaLength };
		_closeEma = new ExponentialMovingAverage { Length = EmaLength };
		_highEma = new ExponentialMovingAverage { Length = EmaLength };
		_lowEma = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_openEma, _closeEma, _highEma, _lowEma, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal openEma, decimal closeEma, decimal highEma, decimal lowEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_openEma.IsFormed || !_closeEma.IsFormed || !_highEma.IsFormed || !_lowEma.IsFormed)
			return;

		var haClose = (openEma + highEma + lowEma + closeEma) / 4m;
		var haOpen = _prevHaOpen is null ? (openEma + closeEma) / 2m : (_prevHaOpen.Value + _prevHaClose!.Value) / 2m;

		var isGreen = haClose >= haOpen;
		var buySignal = isGreen && _prevIsGreen == false;
		var sellSignal = !isGreen && _prevIsGreen == true;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (sellSignal && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevIsGreen = isGreen;
	}
}

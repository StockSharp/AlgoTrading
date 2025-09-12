using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smoothed Heiken-Ashi strategy.
/// Uses EMA-smoothed Heiken-Ashi candles and enters long when the bullish body expands compared to the previous bar.
/// Closes the position when a bearish body expands.
/// </summary>
public class SmoothedHeikenAshiStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _openEma;
	private ExponentialMovingAverage _closeEma;
	private ExponentialMovingAverage _highEma;
	private ExponentialMovingAverage _lowEma;

	private decimal _prevOpenEma;
	private decimal _prevCloseEma;
	private decimal? _prevShaOpen;
	private decimal? _prevShaClose;

	/// <summary>
	/// EMA length for smoothing.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SmoothedHeikenAshiStrategy"/>.
	/// </summary>
	public SmoothedHeikenAshiStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period for smoothing", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevOpenEma = 0m;
		_prevCloseEma = 0m;
		_prevShaOpen = null;
		_prevShaClose = null;
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

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal openEmaValue, decimal closeEmaValue, decimal highEmaValue, decimal lowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_openEma.IsFormed || !_closeEma.IsFormed || !_highEma.IsFormed || !_lowEma.IsFormed)
			return;

		if (_prevShaOpen is null || _prevShaClose is null)
		{
			_prevOpenEma = openEmaValue;
			_prevCloseEma = closeEmaValue;
			_prevShaOpen = (_prevOpenEma + _prevCloseEma) / 2m;
			_prevShaClose = (openEmaValue + highEmaValue + lowEmaValue + closeEmaValue) / 4m;
			return;
		}

		var shaOpen = (_prevOpenEma + _prevCloseEma) / 2m;
		var shaClose = (openEmaValue + highEmaValue + lowEmaValue + closeEmaValue) / 4m;

		var diff = shaClose - shaOpen;
		var prevDiff = _prevShaClose.Value - _prevShaOpen.Value;

		var buySignal = diff > 0m && diff > prevDiff;
		var sellSignal = diff < 0m && diff < prevDiff;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (sellSignal && Position > 0)
		{
			SellMarket(Position);
		}

		_prevOpenEma = openEmaValue;
		_prevCloseEma = closeEmaValue;
		_prevShaOpen = shaOpen;
		_prevShaClose = shaClose;
	}
}


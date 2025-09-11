using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using HMA 200 and EMA 20 crossover of price.
/// </summary>
public class Hma200Ema20CrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hmaLength;
	private readonly StrategyParam<int> _emaLength;

	private HullMovingAverage _hma;
	private ExponentialMovingAverage _ema;
	private decimal _prevClose;
	private decimal _prevEma;
	private bool _hasPrev;

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// HMA period length.
	/// </summary>
	public int HmaLength { get => _hmaLength.Value; set => _hmaLength.Value = value; }

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Hma200Ema20CrossoverStrategy"/> class.
	/// </summary>
	public Hma200Ema20CrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_hmaLength = Param(nameof(HmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("HMA Length", "Hull MA length", "Parameters");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Exponential MA length", "Parameters");
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
		_prevClose = 0m;
		_prevEma = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hma = new HullMovingAverage { Length = HmaLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_hma, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hma.IsFormed || !_ema.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var crossUp = false;
		var crossDown = false;

		if (_hasPrev)
		{
			crossUp = _prevClose <= _prevEma && close > emaValue;
			crossDown = _prevClose >= _prevEma && close < emaValue;
		}

		if (crossUp && close > hmaValue && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (crossDown && close < hmaValue && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = close;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}


using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull moving average candle coloring.
/// </summary>
public class HullCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _bodyLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _bodyHma;
	private SimpleMovingAverage _sma;
	private decimal _prevHma;
	private bool _hasPrev;

	/// <summary>
	/// Hull moving average length for candle body.
	/// </summary>
	public int BodyLength
	{
		get => _bodyLength.Value;
		set => _bodyLength.Value = value;
	}

	/// <summary>
	/// SMA length for close price.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HullCandlesStrategy"/> class.
	/// </summary>
	public HullCandlesStrategy()
	{
		_bodyLength = Param(nameof(BodyLength), 10)
			.SetDisplay("Body HMA", "Hull MA length", "Indicators")
			.SetCanOptimize(true);
		_smaLength = Param(nameof(SmaLength), 1)
			.SetDisplay("Close SMA", "Close SMA length", "Indicators")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHma = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bodyHma = new HullMovingAverage { Length = BodyLength };
		_sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bodyHma);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var avgPrice = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var hmaValue = _bodyHma.Process(new DecimalIndicatorValue(_bodyHma, avgPrice));

		if (!hmaValue.IsFinal || hmaValue is not DecimalIndicatorValue hmaResult)
			return;

		var hma = hmaResult.Value;

		if (_hasPrev)
		{
			var longSignal = hma > _prevHma && candle.ClosePrice > sma;
			var shortSignal = hma < _prevHma && candle.ClosePrice < sma;

			if (longSignal && Position <= 0)
				BuyMarket();
			else if (shortSignal && Position >= 0)
				SellMarket();
		}

		_prevHma = hma;
		_hasPrev = true;
	}
}

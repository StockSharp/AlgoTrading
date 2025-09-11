namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading double Bollinger Bands with entry and exit signals.
/// </summary>
public class DoubleBollingerBandsSignalsStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _width1;
	private readonly StrategyParam<decimal> _width2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevUpper1;
	private decimal _prevLower1;
	private decimal _prevUpper2;
	private decimal _prevLower2;
	private bool _isInitialized;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// First band deviation.
	/// </summary>
	public decimal Width1
	{
		get => _width1.Value;
		set => _width1.Value = value;
	}

	/// <summary>
	/// Second band deviation.
	/// </summary>
	public decimal Width2
	{
		get => _width2.Value;
		set => _width2.Value = value;
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
	/// Initialize the Double Bollinger Bands Signals strategy.
	/// </summary>
	public DoubleBollingerBandsSignalsStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bollinger Bands length", "Bollinger Bands");

		_width1 = Param(nameof(Width1), 2m)
			.SetDisplay("Width1", "First Bollinger Bands deviation", "Bollinger Bands");

		_width2 = Param(nameof(Width2), 3m)
			.SetDisplay("Width2", "Second Bollinger Bands deviation", "Bollinger Bands");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prevClose = default;
		_prevUpper1 = default;
		_prevLower1 = default;
		_prevUpper2 = default;
		_prevLower2 = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bb1 = new BollingerBands
		{
			Length = Length,
			Width = Width1
		};

		var bb2 = new BollingerBands
		{
			Length = Length,
			Width = Width2
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bb1, bb2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb1);
			DrawIndicator(area, bb2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal middle1, decimal upper1, decimal lower1,
		decimal middle2, decimal upper2, decimal lower2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_isInitialized)
		{
			var longCondition = _prevClose < _prevLower2 && close > lower2;
			var shortCondition = _prevClose > _prevUpper2 && close < upper2;
			var exitLong = _prevClose < _prevUpper1 && close > upper1;
			var exitShort = _prevClose > _prevLower1 && close < lower1;

			if (exitLong && Position > 0)
			{
				SellMarket();
			}
			else if (exitShort && Position < 0)
			{
				BuyMarket();
			}
			else if (longCondition && Position <= 0)
			{
				BuyMarket();
			}
			else if (shortCondition && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevClose = close;
		_prevUpper1 = upper1;
		_prevLower1 = lower1;
		_prevUpper2 = upper2;
		_prevLower2 = lower2;
		_isInitialized = true;
	}
}

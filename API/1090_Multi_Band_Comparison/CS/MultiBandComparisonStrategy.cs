using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-Band Comparison Strategy - enters long when price stays above upper quantile minus standard deviation.
/// </summary>
public class MultiBandComparisonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<decimal> _upperQuantile;
	private readonly StrategyParam<int> _entryConfirmBars;
	private readonly StrategyParam<int> _exitConfirmBars;

	private readonly Queue<decimal> _prices = new();
	private int _entryCounter;
	private int _exitCounter;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// SMA period.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Bollinger multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Upper quantile (0-1).
	/// </summary>
	public decimal UpperQuantile { get => _upperQuantile.Value; set => _upperQuantile.Value = value; }

	/// <summary>
	/// Bars required for entry confirmation.
	/// </summary>
	public int EntryConfirmBars { get => _entryConfirmBars.Value; set => _entryConfirmBars.Value = value; }

	/// <summary>
	/// Bars required for exit confirmation.
	/// </summary>
	public int ExitConfirmBars { get => _exitConfirmBars.Value; set => _exitConfirmBars.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiBandComparisonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "SMA period", "Bands")
			.SetGreaterThanZero();

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1m)
			.SetDisplay("BB Mult", "Bollinger multiplier", "Bands")
			.SetGreaterThanZero();

		_upperQuantile = Param(nameof(UpperQuantile), 0.95m)
			.SetDisplay("Upper Quantile", "Quantile for upper band", "Bands")
			.SetGreaterOrEquals(0m)
			.SetLessOrEquals(1m);

		_entryConfirmBars = Param(nameof(EntryConfirmBars), 1)
			.SetDisplay("Entry Confirm Bars", "Bars for entry confirmation", "Trading")
			.SetGreaterThanZero();

		_exitConfirmBars = Param(nameof(ExitConfirmBars), 1)
			.SetDisplay("Exit Confirm Bars", "Bars for exit confirmation", "Trading")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > Length)
			_prices.Dequeue();

		if (_prices.Count < Length || stdValue == 0m)
			return;

		var quantUpper = GetQuantile(_prices, UpperQuantile);
		var quantUpperStdDown = quantUpper - stdValue * BollingerMultiplier;

		var trigger = candle.ClosePrice > quantUpperStdDown;

		_entryCounter = trigger ? _entryCounter + 1 : 0;
		_exitCounter = !trigger ? _exitCounter + 1 : 0;

		if (Position <= 0 && _entryCounter >= EntryConfirmBars)
		{
			BuyMarket();
		}
		else if (Position > 0 && _exitCounter >= ExitConfirmBars)
		{
			SellMarket(Position);
		}
	}

	private static decimal GetQuantile(IEnumerable<decimal> values, decimal q)
	{
		var list = new List<decimal>(values);
		list.Sort();
		var index = (int)Math.Round((list.Count - 1) * q);
		return list[index];
	}
}

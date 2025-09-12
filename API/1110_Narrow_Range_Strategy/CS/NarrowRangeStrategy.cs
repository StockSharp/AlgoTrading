namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading breakouts after a narrow range bar.
/// </summary>
public class NarrowRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highs = [];
	private decimal[] _lows = [];
	private int _index;
	private bool _bufferFilled;

	private bool _longSetup;
	private bool _shortSetup;
	private decimal _longEntry;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortEntry;
	private decimal _shortStop;
	private decimal _shortTake;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Narrow range length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Stop loss percent of reference range.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initialize the Narrow Range strategy.
	/// </summary>
	public NarrowRangeStrategy()
	{
		_length = Param(nameof(Length), 4)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Narrow range length", "Narrow Range")
			.SetCanOptimize();

		_stopLossPercent = Param(nameof(StopLossPercent), 0.35m)
			.SetDisplay("Stop Loss Percent", "Stop loss percent of range", "Narrow Range")
			.SetCanOptimize();

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

		_highs = new decimal[Length];
		_lows = new decimal[Length];
		_index = 0;
		_bufferFilled = false;
		_longSetup = false;
		_shortSetup = false;
		_stopLoss = default;
		_takeProfit = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var oldHigh = _highs[_index];
		var oldLow = _lows[_index];

		_highs[_index] = candle.HighPrice;
		_lows[_index] = candle.LowPrice;

		_index++;
		if (_index >= Length)
		{
			_index = 0;
			_bufferFilled = true;
		}

		if (_bufferFilled)
		{
			var nr = candle.LowPrice > oldLow && candle.HighPrice < oldHigh;

			if (nr && Position == 0)
			{
				var range = oldHigh - oldLow;

				_longEntry = oldHigh;
				_longTake = oldHigh + range;
				_longStop = oldHigh - range * StopLossPercent;

				_shortEntry = oldLow;
				_shortTake = oldLow - range;
				_shortStop = oldLow + range * StopLossPercent;

				_longSetup = true;
				_shortSetup = true;
			}
		}

		if (_longSetup && candle.HighPrice >= _longEntry && Position <= 0)
		{
			BuyMarket();
			_stopLoss = _longStop;
			_takeProfit = _longTake;
			_longSetup = false;
			_shortSetup = false;
		}
		else if (_shortSetup && candle.LowPrice <= _shortEntry && Position >= 0)
		{
			SellMarket();
			_stopLoss = _shortStop;
			_takeProfit = _shortTake;
			_longSetup = false;
			_shortSetup = false;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
			{
				SellMarket();
				_stopLoss = default;
				_takeProfit = default;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
			{
				BuyMarket();
				_stopLoss = default;
				_takeProfit = default;
			}
		}
	}
}

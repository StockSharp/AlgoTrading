namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on smoothed bull and bear power comparison.
/// </summary>
public class BnBStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private ExponentialMovingAverage _bullsMa;
	private ExponentialMovingAverage _bearsMa;
	private decimal? _prevBulls;
	private decimal? _prevBears;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}
	public BnBStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");

		_length = Param(nameof(Length), 14)
			.SetDisplay("EMA Length", "Length of smoothing for bulls and bears", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Distance to stop loss in price points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Distance to take profit in price points", "Risk");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entry", "Enable long position opening", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entry", "Enable short position opening", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Allow Long Exit", "Enable long position closing", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Allow Short Exit", "Enable short position closing", "Trading");
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

		_prevBulls = default;
		_prevBears = default;
		_entryPrice = default;
		_stopPrice = default;
		_targetPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_bullsMa = new ExponentialMovingAverage { Length = Length };
		_bearsMa = new ExponentialMovingAverage { Length = Length };

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

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var volume = candle.TotalVolume ?? candle.Volume ?? 1m;
		if (volume == 0m)
			volume = 1m;

		var tic = (high - low) / volume;
		if (tic == 0m)
			return;

		decimal diff = 0m;
		if (open > close)
			diff = ((high - low) - (open - close)) / (2m * tic);
		else if (open < close)
			diff = ((high - low) - (close - open)) / (2m * tic);

		var bulls = open > close ? (open - close) / tic + diff : diff;
		var bears = open < close ? (close - open) / tic + diff : diff;

		var bullsVal = _bullsMa.Process(candle, bulls);
		var bearsVal = _bearsMa.Process(candle, bears);

		if (!bullsVal.IsFinal || !bearsVal.IsFinal)
			return;

		var bullsSmooth = bullsVal.GetValue<decimal>();
		var bearsSmooth = bearsVal.GetValue<decimal>();

		if (_prevBulls is decimal prevBulls && _prevBears is decimal prevBears)
		{
			var crossUp = prevBulls <= prevBears && bullsSmooth > bearsSmooth;
			var crossDown = prevBulls >= prevBears && bullsSmooth < bearsSmooth;

			var price = close;

			if (crossUp)
			{
				if (SellClose && Position < 0)
					ClosePosition();

				if (BuyOpen && Position <= 0)
				{
					BuyMarket();
					_entryPrice = price;
					_stopPrice = price - StopLoss;
					_targetPrice = price + TakeProfit;
				}
			}
			else if (crossDown)
			{
				if (BuyClose && Position > 0)
					ClosePosition();

				if (SellOpen && Position >= 0)
				{
					SellMarket();
					_entryPrice = price;
					_stopPrice = price + StopLoss;
					_targetPrice = price - TakeProfit;
				}
			}
		}

		if (Position > 0)
		{
			if (close <= _stopPrice || close >= _targetPrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (close >= _stopPrice || close <= _targetPrice)
				ClosePosition();
		}

		_prevBulls = bullsSmooth;
		_prevBears = bearsSmooth;
	}
}

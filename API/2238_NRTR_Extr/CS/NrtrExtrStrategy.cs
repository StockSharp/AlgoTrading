namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// NRTR Extr strategy based on NRTR indicator with extra signals.
/// </summary>
public class NrtrExtrStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _digits;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public int Digits
	{
		get => _digits.Value;
		set => _digits.Value = value;
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

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NrtrExtrStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetDisplay("Period", "NRTR averaging period", "Indicator");

		_digits = Param(nameof(Digits), 0)
			.SetDisplay("Digits Shift", "Additional precision adjustment", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Protective stop in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Profit target in points", "Risk");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable Buy Open", "Allow opening long positions", "Permissions");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable Sell Open", "Allow opening short positions", "Permissions");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Enable Buy Close", "Allow closing long positions", "Permissions");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Enable Sell Close", "Allow closing short positions", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for NRTR calculation", "General");
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

		_stopPrice = default;
		_takePrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(); // enable built-in position protection

		var nrtr = new NrtrExtrIndicator
		{
			Period = Period,
			Digits = Digits
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(nrtr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nrtr = (NrtrExtrValue)value;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if ((SellPosClose && nrtr.SellSignal != 0m) ||
				(_stopPrice != 0m && candle.LowPrice <= _stopPrice) ||
				(_takePrice != 0m && candle.HighPrice >= _takePrice))
			{
				ClosePosition();
			}
		}
		else if (Position < 0)
		{
			if ((BuyPosClose && nrtr.BuySignal != 0m) ||
				(_stopPrice != 0m && candle.HighPrice >= _stopPrice) ||
				(_takePrice != 0m && candle.LowPrice <= _takePrice))
			{
				ClosePosition();
			}
		}
		else
		{
			if (BuyPosOpen && nrtr.BuySignal != 0m)
			{
				BuyMarket();
				_stopPrice = close - StopLoss * Security.PriceStep;
				_takePrice = close + TakeProfit * Security.PriceStep;
			}
			else if (SellPosOpen && nrtr.SellSignal != 0m)
			{
				SellMarket();
				_stopPrice = close + StopLoss * Security.PriceStep;
				_takePrice = close - TakeProfit * Security.PriceStep;
			}
		}
	}
}
/// <summary>
/// NRTR Extr indicator producing trend lines and signals.
/// </summary>
public class NrtrExtrIndicator : BaseIndicator<decimal>
{
	public int Period { get; set; } = 10;
	public int Digits { get; set; }

	private readonly AverageTrueRange _atr = new();
	private decimal _price;
	private decimal _value;
	private int _trend;
	private int _trendPrev;
	private bool _initialized;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		if (_atr.Length != Period)
			_atr.Length = Period;

		var atrVal = _atr.Process(input).ToDecimal();
		if (!_initialized)
		{
			_price = candle.ClosePrice;
			_value = candle.ClosePrice;
			_trend = 0;
			_trendPrev = 0;
			_initialized = true;
			return new NrtrExtrValue(this, input, 0m, 0m, 0m, 0m);
		}

		var dK = atrVal / (decimal)Math.Pow(10, Digits);

		if (_trend >= 0)
		{
			_price = Math.Max(_price, candle.HighPrice);
			_value = Math.Max(_value, _price * (1m - dK));

			if (candle.HighPrice < _value)
			{
				_price = candle.HighPrice;
				_value = _price * (1m + dK);
				_trend = -1;
			}
		}
		else
		{
			_price = Math.Min(_price, candle.LowPrice);
			_value = Math.Min(_value, _price * (1m + dK));

			if (candle.LowPrice > _value)
			{
				_price = candle.LowPrice;
				_value = _price * (1m - dK);
				_trend = 1;
			}
		}

		decimal trendUp = 0m, trendDown = 0m, buySignal = 0m, sellSignal = 0m;

		if (_trend > 0)
			trendUp = _value;
		else if (_trend < 0)
			trendDown = _value;

		if (_trendPrev < 0 && _trend > 0)
			buySignal = trendUp;

		if (_trendPrev > 0 && _trend < 0)
			sellSignal = trendDown;

		_trendPrev = _trend;

		return new NrtrExtrValue(this, input, trendUp, trendDown, buySignal, sellSignal);
	}
}

/// <summary>
/// Indicator value for <see cref="NrtrExtrIndicator"/>.
/// </summary>
public class NrtrExtrValue : ComplexIndicatorValue
{
	public NrtrExtrValue(IIndicator indicator, IIndicatorValue input, decimal upTrend, decimal downTrend, decimal buySignal, decimal sellSignal)
		: base(indicator, input, (nameof(UpTrend), upTrend), (nameof(DownTrend), downTrend), (nameof(BuySignal), buySignal), (nameof(SellSignal), sellSignal))
	{
	}

	/// <summary>
	/// Upper trend line value.
	/// </summary>
	public decimal UpTrend => (decimal)GetValue(nameof(UpTrend));

	/// <summary>
	/// Lower trend line value.
	/// </summary>
	public decimal DownTrend => (decimal)GetValue(nameof(DownTrend));

	/// <summary>
	/// Buy signal value.
	/// </summary>
	public decimal BuySignal => (decimal)GetValue(nameof(BuySignal));

	/// <summary>
	/// Sell signal value.
	/// </summary>
	public decimal SellSignal => (decimal)GetValue(nameof(SellSignal));
}

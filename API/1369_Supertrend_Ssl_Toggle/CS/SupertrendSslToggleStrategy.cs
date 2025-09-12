using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend and SSL strategy with optional confirmation between indicators.
/// </summary>
public class SupertrendSslToggleStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _sslPeriod;
	private readonly StrategyParam<bool> _useConfirmation;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaHigh;
	private SimpleMovingAverage _smaLow;
	private decimal _hlv;
	private decimal _sslUp;
	private decimal _sslDown;
	private decimal _prevSslUp;
	private decimal _prevSslDown;

	private bool _prevDirectionUp;
	private bool _waitForSslBuy;
	private bool _waitForSslSell;
	private bool _waitForStBuy;
	private bool _waitForStSell;

	/// <summary>
	/// ATR period for Supertrend.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// Period for SSL channel.
	/// </summary>
	public int SslPeriod { get => _sslPeriod.Value; set => _sslPeriod.Value = value; }

	/// <summary>
	/// Require confirmation from both indicators.
	/// </summary>
	public bool UseConfirmation { get => _useConfirmation.Value; set => _useConfirmation.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="SupertrendSslToggleStrategy"/>.
	/// </summary>
	public SupertrendSslToggleStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend");

		_factor = Param(nameof(Factor), 2.4m)
			.SetDisplay("Factor", "ATR multiplier for Supertrend", "Supertrend");

		_sslPeriod = Param(nameof(SslPeriod), 13)
			.SetDisplay("SSL Period", "Period for SSL channel", "SSL");

		_useConfirmation = Param(nameof(UseConfirmation), true)
			.SetDisplay("Use Confirmation", "Require both signals", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevDirectionUp = false;
		_hlv = 0m;
		_sslUp = _sslDown = _prevSslUp = _prevSslDown = 0m;
		_waitForSslBuy = _waitForSslSell = _waitForStBuy = _waitForStSell = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smaHigh = new SimpleMovingAverage { Length = SslPeriod };
		_smaLow = new SimpleMovingAverage { Length = SslPeriod };
		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(st, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st = stValue.ToDecimal();
		var directionUp = candle.ClosePrice > st;
		var supertrendBuy = directionUp && !_prevDirectionUp;
		var supertrendSell = !directionUp && _prevDirectionUp;
		_prevDirectionUp = directionUp;

		var highValue = _smaHigh.Process(new DecimalIndicatorValue(_smaHigh, candle.HighPrice));
		var lowValue = _smaLow.Process(new DecimalIndicatorValue(_smaLow, candle.LowPrice));
		if (!_smaHigh.IsFormed || !_smaLow.IsFormed)
			return;

		var smaHigh = highValue.ToDecimal();
		var smaLow = lowValue.ToDecimal();

		if (candle.ClosePrice > smaHigh)
			_hlv = 1m;
		else if (candle.ClosePrice < smaLow)
			_hlv = -1m;

		_sslDown = _hlv < 0 ? smaHigh : smaLow;
		_sslUp = _hlv < 0 ? smaLow : smaHigh;

		var sslBuy = _sslUp > _sslDown && _prevSslUp <= _prevSslDown;
		var sslSell = _sslDown > _sslUp && _prevSslDown <= _prevSslUp;

		if (UseConfirmation)
		{
			if (sslBuy && !_waitForStBuy)
				_waitForSslBuy = true;
			if (supertrendBuy && !_waitForSslBuy)
				_waitForStBuy = true;

			if (sslBuy && _waitForStBuy && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_waitForStBuy = _waitForSslBuy = false;
			}
			if (supertrendBuy && _waitForSslBuy && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_waitForStBuy = _waitForSslBuy = false;
			}

			if (sslSell && !_waitForStSell)
				_waitForSslSell = true;
			if (supertrendSell && !_waitForSslSell)
				_waitForStSell = true;

			if (sslSell && _waitForStSell && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_waitForStSell = _waitForSslSell = false;
			}
			if (supertrendSell && _waitForSslSell && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_waitForStSell = _waitForSslSell = false;
			}

			if (Position > 0 && (sslSell || supertrendSell))
			{
				SellMarket(Position);
				_waitForStBuy = _waitForSslBuy = false;
			}
			else if (Position < 0 && (sslBuy || supertrendBuy))
			{
				BuyMarket(Math.Abs(Position));
				_waitForStSell = _waitForSslSell = false;
			}
		}
		else
		{
			if ((sslBuy || supertrendBuy) && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			if ((sslSell || supertrendSell) && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (Position > 0 && (sslSell || supertrendSell))
				SellMarket(Position);
			else if (Position < 0 && (sslBuy || supertrendBuy))
				BuyMarket(Math.Abs(Position));
		}

		_prevSslUp = _sslUp;
		_prevSslDown = _sslDown;
	}
}

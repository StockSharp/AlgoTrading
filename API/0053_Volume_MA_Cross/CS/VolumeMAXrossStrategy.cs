using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume MA Cross strategy.
/// Uses fast/slow volume MA crossover with price MA for direction.
/// Long: Volume expanding and price above SMA.
/// Short: Volume expanding and price below SMA.
/// </summary>
public class VolumeMAXrossStrategy : Strategy
{
	private readonly StrategyParam<int> _priceMaPeriod;
	private readonly StrategyParam<int> _fastVolPeriod;
	private readonly StrategyParam<int> _slowVolPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _fastVolMA;
	private SimpleMovingAverage _slowVolMA;
	private decimal _prevClose;
	private decimal _prevMa;
	private int _cooldown;

	/// <summary>
	/// Price MA Period.
	/// </summary>
	public int PriceMaPeriod
	{
		get => _priceMaPeriod.Value;
		set => _priceMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast Volume MA Period.
	/// </summary>
	public int FastVolPeriod
	{
		get => _fastVolPeriod.Value;
		set => _fastVolPeriod.Value = value;
	}

	/// <summary>
	/// Slow Volume MA Period.
	/// </summary>
	public int SlowVolPeriod
	{
		get => _slowVolPeriod.Value;
		set => _slowVolPeriod.Value = value;
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
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="VolumeMAXrossStrategy"/>.
	/// </summary>
	public VolumeMAXrossStrategy()
	{
		_priceMaPeriod = Param(nameof(PriceMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Period", "Period for price SMA", "Indicators");

		_fastVolPeriod = Param(nameof(FastVolPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Vol Period", "Period for fast volume MA", "Indicators");

		_slowVolPeriod = Param(nameof(SlowVolPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Vol Period", "Period for slow volume MA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_fastVolMA = null;
		_slowVolMA = null;
		_prevClose = default;
		_prevMa = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevMa = 0;
		_cooldown = 0;

		_fastVolMA = new SimpleMovingAverage { Length = FastVolPeriod };
		_slowVolMA = new SimpleMovingAverage { Length = SlowVolPeriod };

		var sma = new SimpleMovingAverage { Length = PriceMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Process volume through manual MAs
		var fastVol = _fastVolMA.Process(new DecimalIndicatorValue(_fastVolMA, candle.TotalVolume, candle.ServerTime)).ToDecimal();
		var slowVol = _slowVolMA.Process(new DecimalIndicatorValue(_slowVolMA, candle.TotalVolume, candle.ServerTime)).ToDecimal();

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = smaValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			_prevMa = smaValue;
			return;
		}

		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > smaValue;
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < smaValue;
		var volumeExpanding = _slowVolMA.IsFormed && fastVol > slowVol;

		if (Position == 0 && crossUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && crossDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && (crossDown || (volumeExpanding && candle.ClosePrice < smaValue)))
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (crossUp || (volumeExpanding && candle.ClosePrice > smaValue)))
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevMa = smaValue;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Surge strategy.
/// Long: Price above MA with volume confirmation.
/// Short: Price below MA with volume confirmation.
/// Exit: Price crosses MA.
/// </summary>
public class VolumeSurgeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private decimal _prevMa;
	private decimal _prevVolume;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initialize <see cref="VolumeSurgeStrategy"/>.
	/// </summary>
	public VolumeSurgeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for price MA", "Indicators")
			.SetOptimize(10, 50, 10);

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
		_prevClose = default;
		_prevMa = default;
		_prevVolume = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevMa = 0;
		_prevVolume = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			_prevVolume = candle.TotalVolume;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			_prevVolume = candle.TotalVolume;
			return;
		}

		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > maValue;
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < maValue;
		var volumeRising = candle.TotalVolume > _prevVolume;

		if (Position == 0 && crossUp && volumeRising)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && crossDown && volumeRising)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && crossDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevMa = maValue;
		_prevVolume = candle.TotalVolume;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX based long-only strategy for BTC.
/// Enters when ADX crosses above entry level, exits when ADX crosses below exit level.
/// </summary>
public class AdxForBtcStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevAdx;
	private int _cooldownRemaining;

	public decimal EntryLevel { get => _entryLevel.Value; set => _entryLevel.Value = value; }
	public decimal ExitLevel { get => _exitLevel.Value; set => _exitLevel.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdxForBtcStrategy()
	{
		_entryLevel = Param(nameof(EntryLevel), 14m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Level", "ADX threshold for entry", "Strategy");

		_exitLevel = Param(nameof(ExitLevel), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Level", "ADX threshold for exit", "Strategy");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length for trend SMA", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAdx = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = 14 };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxMa ||
			adxTyped.Dx.Plus is not decimal diPlus ||
			adxTyped.Dx.Minus is not decimal diMinus)
			return;

		var smaVal = smaValue.ToDecimal();

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevAdx = adxMa;
			return;
		}

		// Enter long when ADX crosses above entry level with +DI > -DI and price above SMA
		if (_prevAdx > 0 && _prevAdx <= EntryLevel && adxMa > EntryLevel && diPlus > diMinus && candle.ClosePrice > smaVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Enter short when ADX crosses above entry level with -DI > +DI and price below SMA
		else if (_prevAdx > 0 && _prevAdx <= EntryLevel && adxMa > EntryLevel && diMinus > diPlus && candle.ClosePrice < smaVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit when ADX drops below exit level
		else if (Position > 0 && adxMa < ExitLevel && _prevAdx >= ExitLevel)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && adxMa < ExitLevel && _prevAdx >= ExitLevel)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevAdx = adxMa;
	}
}

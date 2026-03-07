namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MA Cross + DMI Strategy.
/// Uses MA crossover confirmed by DMI directional alignment.
/// Buys when fast MA crosses above slow MA and DI+ > DI-.
/// Sells when fast MA crosses below slow MA and DI- > DI+.
/// </summary>
public class MaCrossDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ma1;
	private ExponentialMovingAverage _ma2;
	private DirectionalIndex _dmi;

	private decimal _prevMa1;
	private decimal _prevMa2;
	private int _cooldownRemaining;

	public MaCrossDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Fast moving average period", "Moving Average");

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Slow moving average period", "Moving Average");

		_dmiLength = Param(nameof(DmiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Length", "DMI period", "DMI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1 = null;
		_ma2 = null;
		_dmi = null;
		_prevMa1 = 0;
		_prevMa2 = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma1 = new ExponentialMovingAverage { Length = Ma1Length };
		_ma2 = new ExponentialMovingAverage { Length = Ma2Length };
		_dmi = new DirectionalIndex { Length = DmiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ma1, _ma2, _dmi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma1.IsFormed || !_ma2.IsFormed || !_dmi.IsFormed)
			return;

		if (ma1Value.IsEmpty || ma2Value.IsEmpty || dmiValue.IsEmpty)
			return;

		var ma1Price = ma1Value.ToDecimal();
		var ma2Price = ma2Value.ToDecimal();

		var dmiData = (DirectionalIndexValue)dmiValue;
		if (dmiData.Plus is not decimal diPlus || dmiData.Minus is not decimal diMinus)
		{
			_prevMa1 = ma1Price;
			_prevMa2 = ma2Price;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMa1 = ma1Price;
			_prevMa2 = ma2Price;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMa1 = ma1Price;
			_prevMa2 = ma2Price;
			return;
		}

		if (_prevMa1 == 0 || _prevMa2 == 0)
		{
			_prevMa1 = ma1Price;
			_prevMa2 = ma2Price;
			return;
		}

		// MA crossover detection
		var maCrossUp = ma1Price > ma2Price && _prevMa1 <= _prevMa2;
		var maCrossDown = ma1Price < ma2Price && _prevMa1 >= _prevMa2;

		// Buy: MA cross up + DI+ > DI- (bullish direction)
		if (maCrossUp && diPlus > diMinus && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: MA cross down + DI- > DI+ (bearish direction)
		else if (maCrossDown && diMinus > diPlus && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long on MA cross down (without DMI requirement)
		else if (Position > 0 && maCrossDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short on MA cross up (without DMI requirement)
		else if (Position < 0 && maCrossUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevMa1 = ma1Price;
		_prevMa2 = ma2Price;
	}
}

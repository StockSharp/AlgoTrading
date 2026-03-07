namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Squeeze Pro Overlays Strategy.
/// Detects when BB is inside KC (squeeze), then trades on breakout direction.
/// Uses momentum (LinearRegSlope) to determine direction.
/// </summary>
public class SqueezeProOverlaysStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _squeezeLength;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bb;
	private KeltnerChannels _kc;
	private LinearRegSlope _slope;

	private bool _wasSqueezed;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SqueezeLength
	{
		get => _squeezeLength.Value;
		set => _squeezeLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SqueezeProOverlaysStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_squeezeLength = Param(nameof(SqueezeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Squeeze Length", "Calculation length", "Squeeze");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bb = null;
		_kc = null;
		_slope = null;
		_wasSqueezed = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bb = new BollingerBands { Length = SqueezeLength, Width = 2m };
		_kc = new KeltnerChannels { Length = SqueezeLength, Multiplier = 1.5m };
		_slope = new LinearRegSlope { Length = SqueezeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bb, _kc, _slope, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue kcValue, IIndicatorValue slopeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bb.IsFormed || !_kc.IsFormed || !_slope.IsFormed)
			return;

		if (bbValue.IsEmpty || kcValue.IsEmpty || slopeValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbValue;
		var kc = (KeltnerChannelsValue)kcValue;

		if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower)
			return;
		if (kc.Upper is not decimal kcUpper || kc.Lower is not decimal kcLower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var slopeVal = slopeValue.ToDecimal();
		var squeezed = bbUpper < kcUpper && bbLower > kcLower;

		// Squeeze release: was squeezed, now not
		if (_wasSqueezed && !squeezed)
		{
			if (slopeVal > 0 && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
			else if (slopeVal < 0 && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}
		// Exit: slope reverses
		else if (Position > 0 && slopeVal < 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && slopeVal > 0)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_wasSqueezed = squeezed;
	}
}

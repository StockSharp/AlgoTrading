namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid Bot Strategy.
/// Creates a dynamic grid around a moving average and trades grid crossings.
/// Buys when price crosses below a grid line, sells when price crosses above.
/// </summary>
public class GridBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _gridCount;
	private readonly StrategyParam<decimal> _gridMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ma;
	private AverageTrueRange _atr;
	private decimal _prevClose;
	private int _cooldownRemaining;

	public GridBotStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average for grid center", "Grid Settings");

		_atrLength = Param(nameof(ATRLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for grid spacing", "Grid Settings");

		_gridCount = Param(nameof(GridCount), 3)
			.SetDisplay("Grid Count", "Number of grid levels each side", "Grid Settings");

		_gridMultiplier = Param(nameof(GridMultiplier), 0.5m)
			.SetDisplay("Grid Multiplier", "ATR multiplier for grid spacing", "Grid Settings");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int MALength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public int ATRLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int GridCount
	{
		get => _gridCount.Value;
		set => _gridCount.Value = value;
	}

	public decimal GridMultiplier
	{
		get => _gridMultiplier.Value;
		set => _gridMultiplier.Value = value;
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

		_ma = null;
		_atr = null;
		_prevClose = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new ExponentialMovingAverage { Length = MALength };
		_atr = new AverageTrueRange { Length = ATRLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, _atr, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma.IsFormed || !_atr.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_prevClose == 0 || atrValue <= 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var close = candle.ClosePrice;
		var gridSpacing = atrValue * GridMultiplier;

		// Check grid crossings
		for (var i = 1; i <= GridCount; i++)
		{
			var lowerGrid = maValue - gridSpacing * i;
			var upperGrid = maValue + gridSpacing * i;

			// Price crossed below a lower grid line - buy
			if (_prevClose > lowerGrid && close <= lowerGrid && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
				_prevClose = close;
				return;
			}

			// Price crossed above an upper grid line - sell
			if (_prevClose < upperGrid && close >= upperGrid && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
				_prevClose = close;
				return;
			}
		}

		// Mean reversion exits at MA
		if (Position > 0 && _prevClose < maValue && close >= maValue)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && _prevClose > maValue && close <= maValue)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = close;
	}
}

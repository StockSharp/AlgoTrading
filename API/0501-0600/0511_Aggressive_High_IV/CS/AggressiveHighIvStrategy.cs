using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aggressive strategy for high implied volatility markets.
/// Uses EMA crossover with ATR volatility filter and ATR-based exits.
/// </summary>
public class AggressiveHighIvStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AggressiveHighIvStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period for fast EMA", "Parameters");

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period for slow EMA", "Parameters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Parameters");

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
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastEma;
			_prevSlow = slowEma;
			return;
		}

		if (_prevFast == 0)
		{
			_prevFast = fastEma;
			_prevSlow = slowEma;
			return;
		}

		// Check ATR-based stop/take for existing positions
		if (Position > 0 && _entryPrice > 0 && atrValue > 0)
		{
			if (candle.ClosePrice <= _entryPrice - 2m * atrValue || candle.ClosePrice >= _entryPrice + 4m * atrValue)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				_prevFast = fastEma;
				_prevSlow = slowEma;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && atrValue > 0)
		{
			if (candle.ClosePrice >= _entryPrice + 2m * atrValue || candle.ClosePrice <= _entryPrice - 4m * atrValue)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				_prevFast = fastEma;
				_prevSlow = slowEma;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevFast = fastEma;
			_prevSlow = slowEma;
			return;
		}

		var longCross = _prevFast <= _prevSlow && fastEma > slowEma;
		var shortCross = _prevFast >= _prevSlow && fastEma < slowEma;

		if (longCross && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		else if (shortCross && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}

		_prevFast = fastEma;
		_prevSlow = slowEma;
	}
}

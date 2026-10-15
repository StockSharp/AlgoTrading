namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// SMA Pullback with ATR Exits Strategy.
/// Buys on pullbacks in uptrend and sells on pullbacks in downtrend.
/// Exits are based on ATR multiples.
/// </summary>
public class SmaPullbackAtrExitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastSmaLength { get => _fastSmaLength.Value; set => _fastSmaLength.Value = value; }
	public int SlowSmaLength { get => _slowSmaLength.Value; set => _slowSmaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public SmaPullbackAtrExitsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastSmaLength = Param(nameof(FastSmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "Indicators");

		_slowSmaLength = Param(nameof(SlowSmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Indicators");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.2m)
			.SetDisplay("ATR SL Mult", "ATR multiplier for stop-loss", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2.0m)
			.SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk");

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
		_entryPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSmaValue, decimal slowSmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check stop/TP exits first
		if (Position > 0 && _entryPrice > 0)
		{
			var stop = _entryPrice - atrValue * AtrMultiplierSl;
			var target = _entryPrice + atrValue * AtrMultiplierTp;

			if (candle.LowPrice <= stop || candle.HighPrice >= target)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stop = _entryPrice + atrValue * AtrMultiplierSl;
			var target = _entryPrice - atrValue * AtrMultiplierTp;

			if (candle.HighPrice >= stop || candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var currentPrice = candle.ClosePrice;

		// Buy: pullback in uptrend (price below fast SMA, fast > slow)
		if (currentPrice < fastSmaValue && fastSmaValue > slowSmaValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = currentPrice;
			_cooldownRemaining = CooldownBars;
		}
		// Sell: pullback in downtrend (price above fast SMA, fast < slow)
		else if (currentPrice > fastSmaValue && fastSmaValue < slowSmaValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = currentPrice;
			_cooldownRemaining = CooldownBars;
		}
	}
}

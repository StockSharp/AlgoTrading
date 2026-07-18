using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vegas SuperTrend strategy using SMA and RSI for trend following.
/// </summary>
public class MultiStepVegasSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _entryRsiLevel;
	private readonly StrategyParam<decimal> _exitRsiLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private decimal? _prevClose;
	private decimal? _prevSma;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal EntryRsiLevel { get => _entryRsiLevel.Value; set => _entryRsiLevel.Value = value; }
	public decimal ExitRsiLevel { get => _exitRsiLevel.Value; set => _exitRsiLevel.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public MultiStepVegasSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");
		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period", "Parameters");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");
		_entryRsiLevel = Param(nameof(EntryRsiLevel), 55m)
			.SetDisplay("Entry RSI", "RSI threshold for long entries", "Parameters");
		_exitRsiLevel = Param(nameof(ExitRsiLevel), 45m)
			.SetDisplay("Exit RSI", "RSI threshold for exits", "Parameters");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 3)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new entry", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = null;
		_prevSma = null;
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevSma = null;
		_cooldownRemaining = 0;

		var sma = new SMA { Length = SmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, rsi, (candle, smaVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (_cooldownRemaining > 0)
					_cooldownRemaining--;

				if (!sma.IsFormed || !rsi.IsFormed || _prevClose is null || _prevSma is null)
				{
					_prevClose = candle.ClosePrice;
					_prevSma = smaVal;
					return;
				}

				var longEntry = _cooldownRemaining == 0 &&
					_prevClose.Value <= _prevSma.Value &&
					candle.ClosePrice > smaVal &&
					rsiVal >= EntryRsiLevel &&
					Position <= 0;

				var longExit = Position > 0 &&
					(candle.ClosePrice < smaVal || rsiVal <= ExitRsiLevel);

				if (longExit)
				{
					SellMarket(Position);
					_cooldownRemaining = SignalCooldownBars;
				}
				else if (longEntry)
				{
					BuyMarket(Volume + (Position < 0 ? -Position : 0m));
					_cooldownRemaining = SignalCooldownBars;
				}

				_prevClose = candle.ClosePrice;
				_prevSma = smaVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal Trap Sniper strategy.
/// Enters on RSI trap patterns and manages exits using ATR based stop and target.
/// </summary>
public class ReversalTrapSniperStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxBars;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private decimal? _rsi1;
	private decimal? _rsi2;
	private decimal? _rsi3;
	private decimal? _prevClose;

	private decimal? _entryPrice;
	private int _barsInPosition;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Overbought RSI level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// Oversold RSI level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Risk-reward ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Maximum holding bars.
	/// </summary>
	public int MaxBars { get => _maxBars.Value; set => _maxBars.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ReversalTrapSniperStrategy"/>.
	/// </summary>
	public ReversalTrapSniperStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General")
			.SetCanOptimize(true);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "General")
			.SetCanOptimize(true);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "General")
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Risk-reward ratio", "General")
			.SetCanOptimize(true);

		_maxBars = Param(nameof(MaxBars), 30)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars", "Maximum bars to hold", "General")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_rsi = default;
		_atr = default;
		_rsi1 = default;
		_rsi2 = default;
		_rsi3 = default;
		_prevClose = default;
		_entryPrice = default;
		_barsInPosition = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, _atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsi3 = _rsi2;
		_rsi2 = _rsi1;
		_rsi1 = rsi;

		bool trapLong = _rsi3.HasValue && _rsi3.Value > RsiOverbought && rsi < RsiOverbought && _prevClose.HasValue && candle.ClosePrice > _prevClose.Value;
		bool trapShort = _rsi3.HasValue && _rsi3.Value < RsiOversold && rsi > RsiOversold && _prevClose.HasValue && candle.ClosePrice < _prevClose.Value;

		_prevClose = candle.ClosePrice;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (trapLong && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsInPosition = 0;
			}
			else if (trapShort && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsInPosition = 0;
			}
		}

		if (Position != 0 && _entryPrice != null)
		{
			_barsInPosition++;

			var stop = Position > 0 ? _entryPrice.Value - atr : _entryPrice.Value + atr;
			var target = Position > 0 ? _entryPrice.Value + atr * RiskReward : _entryPrice.Value - atr * RiskReward;

			bool stopHit = Position > 0 ? candle.LowPrice <= stop : candle.HighPrice >= stop;
			bool targetHit = Position > 0 ? candle.HighPrice >= target : candle.LowPrice <= target;

			if (stopHit || targetHit || _barsInPosition >= MaxBars)
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_entryPrice = null;
				_barsInPosition = 0;
			}
		}
	}
}

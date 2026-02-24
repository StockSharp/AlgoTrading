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
/// Momentum strategy based on WOC 0.1.2 concept.
/// Detects consecutive candle close runs in one direction and enters on breakout.
/// Uses ATR-based stop loss and trailing stop.
/// </summary>
public class Woc012Strategy : Strategy
{
	private readonly StrategyParam<int> _sequenceLength;
	private readonly StrategyParam<decimal> _stopLossAtrMult;
	private readonly StrategyParam<decimal> _trailingAtrMult;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private int _upCount;
	private int _downCount;
	private decimal _entryPrice;
	private decimal? _stopPrice;

	public int SequenceLength { get => _sequenceLength.Value; set => _sequenceLength.Value = value; }
	public decimal StopLossAtrMult { get => _stopLossAtrMult.Value; set => _stopLossAtrMult.Value = value; }
	public decimal TrailingAtrMult { get => _trailingAtrMult.Value; set => _trailingAtrMult.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Woc012Strategy()
	{
		_sequenceLength = Param(nameof(SequenceLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Sequence Length", "Consecutive bars in same direction to trigger entry", "Signals");

		_stopLossAtrMult = Param(nameof(StopLossAtrMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL ATR Mult", "Stop loss as ATR multiple", "Risk");

		_trailingAtrMult = Param(nameof(TrailingAtrMult), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Trail ATR Mult", "Trailing stop as ATR multiple", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation length", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_upCount = 0;
		_downCount = 0;
		_entryPrice = 0;
		_stopPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Track consecutive direction
		if (_prevClose > 0)
		{
			if (close > _prevClose)
			{
				_upCount++;
				_downCount = 0;
			}
			else if (close < _prevClose)
			{
				_downCount++;
				_upCount = 0;
			}
			else
			{
				_upCount = 0;
				_downCount = 0;
			}
		}

		_prevClose = close;

		// Manage existing position
		if (Position != 0)
		{
			if (Position > 0)
			{
				// Trail up
				var trail = close - TrailingAtrMult * atr;
				if (_stopPrice == null || trail > _stopPrice)
					_stopPrice = trail;

				if (close <= _stopPrice)
				{
					SellMarket(Math.Abs(Position));
					_stopPrice = null;
					_entryPrice = 0;
					return;
				}
			}
			else
			{
				// Trail down
				var trail = close + TrailingAtrMult * atr;
				if (_stopPrice == null || trail < _stopPrice)
					_stopPrice = trail;

				if (close >= _stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_stopPrice = null;
					_entryPrice = 0;
					return;
				}
			}
		}

		// Entry: consecutive sequence completed
		if (_upCount >= SequenceLength && Position <= 0)
		{
			var vol = Volume + Math.Abs(Position);
			BuyMarket(vol);
			_entryPrice = close;
			_stopPrice = close - StopLossAtrMult * atr;
			_upCount = 0;
		}
		else if (_downCount >= SequenceLength && Position >= 0)
		{
			var vol = Volume + Math.Abs(Position);
			SellMarket(vol);
			_entryPrice = close;
			_stopPrice = close + StopLossAtrMult * atr;
			_downCount = 0;
		}
	}
}

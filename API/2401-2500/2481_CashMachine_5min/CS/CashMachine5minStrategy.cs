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
/// Cash Machine strategy: combines DeMarker and Stochastic oscillator crossovers
/// with staged profit protection using ATR-based distances.
/// </summary>
public class CashMachine5minStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tpAtrMult;
	private readonly StrategyParam<decimal> _slAtrMult;
	private readonly StrategyParam<decimal> _trailAtrMult;
	private readonly StrategyParam<int> _deMarkerLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDeMarker;
	private decimal? _prevRsi;
	private decimal _entryPrice;
	private decimal? _stopPrice;

	public decimal TpAtrMult { get => _tpAtrMult.Value; set => _tpAtrMult.Value = value; }
	public decimal SlAtrMult { get => _slAtrMult.Value; set => _slAtrMult.Value = value; }
	public decimal TrailAtrMult { get => _trailAtrMult.Value; set => _trailAtrMult.Value = value; }
	public int DeMarkerLength { get => _deMarkerLength.Value; set => _deMarkerLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CashMachine5minStrategy()
	{
		_tpAtrMult = Param(nameof(TpAtrMult), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP ATR Mult", "Take profit ATR multiplier", "Risk");

		_slAtrMult = Param(nameof(SlAtrMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL ATR Mult", "Stop loss ATR multiplier", "Risk");

		_trailAtrMult = Param(nameof(TrailAtrMult), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Trail ATR Mult", "Trailing stop ATR multiplier", "Risk");

		_deMarkerLength = Param(nameof(DeMarkerLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Length", "DeMarker period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDeMarker = null;
		_prevRsi = null;
		_entryPrice = 0;
		_stopPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var deMarker = new DeMarker { Length = DeMarkerLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(deMarker, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarker, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Manage position
		if (Position > 0)
		{
			// Trailing stop
			var trail = close - TrailAtrMult * atr;
			if (_stopPrice == null || trail > _stopPrice)
				_stopPrice = trail;

			// Take profit
			var tp = _entryPrice + TpAtrMult * atr;

			if (close <= _stopPrice || close >= tp)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = null;
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var trail = close + TrailAtrMult * atr;
			if (_stopPrice == null || trail < _stopPrice)
				_stopPrice = trail;

			var tp = _entryPrice - TpAtrMult * atr;

			if (close >= _stopPrice || close <= tp)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
				_entryPrice = 0;
			}
		}

		// Entry signals
		if (Position == 0 && _prevDeMarker is decimal prevDe && _prevRsi is decimal prevRsi)
		{
			var longSignal = (prevDe < 0.25m && deMarker >= 0.25m) || (prevRsi < 25m && rsi >= 25m);
			var shortSignal = (prevDe > 0.75m && deMarker <= 0.75m) || (prevRsi > 75m && rsi <= 75m);

			if (longSignal)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - SlAtrMult * atr;
			}
			else if (shortSignal)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + SlAtrMult * atr;
			}
		}

		_prevDeMarker = deMarker;
		_prevRsi = rsi;
	}
}

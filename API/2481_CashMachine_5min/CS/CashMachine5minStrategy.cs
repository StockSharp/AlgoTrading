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
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDeMarker;
	private decimal? _prevStochK;
	private decimal _entryPrice;
	private decimal? _stopPrice;

	public decimal TpAtrMult { get => _tpAtrMult.Value; set => _tpAtrMult.Value = value; }
	public decimal SlAtrMult { get => _slAtrMult.Value; set => _slAtrMult.Value = value; }
	public decimal TrailAtrMult { get => _trailAtrMult.Value; set => _trailAtrMult.Value = value; }
	public int DeMarkerLength { get => _deMarkerLength.Value; set => _deMarkerLength.Value = value; }
	public int StochasticLength { get => _stochasticLength.Value; set => _stochasticLength.Value = value; }
	public int StochasticD { get => _stochasticD.Value; set => _stochasticD.Value = value; }
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

		_stochasticLength = Param(nameof(StochasticLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Stochastic %K period", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Indicators");

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
		_prevDeMarker = null;
		_prevStochK = null;
		_entryPrice = 0;
		_stopPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var deMarker = new DeMarker { Length = DeMarkerLength };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochasticLength },
			D = { Length = StochasticD },
		};
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(deMarker, stochastic, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue deMarkerVal, IIndicatorValue stochVal, IIndicatorValue atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var deMarker = deMarkerVal.ToDecimal();
		var stoch = (IStochasticOscillatorValue)stochVal;
		var stochK = stoch.K ?? 0m;
		var atr = atrVal.ToDecimal();
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
		if (Position == 0 && _prevDeMarker is decimal prevDe && _prevStochK is decimal prevStoch)
		{
			// Relaxed: DeMarker oversold/overbought OR Stochastic oversold/overbought
			var longSignal = (prevDe < 0.30m && deMarker >= 0.30m) || (prevStoch < 25m && stochK >= 25m);
			var shortSignal = (prevDe > 0.70m && deMarker <= 0.70m) || (prevStoch > 75m && stochK <= 75m);

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
		_prevStochK = stochK;
	}
}

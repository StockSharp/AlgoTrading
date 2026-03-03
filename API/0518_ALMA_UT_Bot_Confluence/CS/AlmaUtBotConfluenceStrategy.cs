using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines ALMA filter with UT Bot trailing stop.
/// Enters long when UT Bot gives a buy signal above EMA.
/// Short entries occur on UT Bot sell signals below EMA.
/// Exits are handled by UT Bot trailing stop or ATR-based stop/target.
/// </summary>
public class AlmaUtBotConfluenceStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<int> _utKeyValue;
	private readonly StrategyParam<int> _utAtrPeriod;
	private readonly StrategyParam<int> _baseCooldownBars;
	private readonly StrategyParam<bool> _useUtExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _xAtrTrailingStop;
	private decimal _prevSrc;
	private decimal _prevStop;
	private int _barIndex;
	private int? _lastTradeIndex;
	private decimal _entryPrice;

	/// <summary>
	/// Length for long-term EMA (default: 72).
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// ATR length (default: 14).
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss (default: 5.0).
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit (default: 4.0).
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// UT Bot key value (default: 2).
	/// </summary>
	public int UtKeyValue
	{
		get => _utKeyValue.Value;
		set => _utKeyValue.Value = value;
	}

	/// <summary>
	/// ATR period for UT Bot (default: 10).
	/// </summary>
	public int UtAtrPeriod
	{
		get => _utAtrPeriod.Value;
		set => _utAtrPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown in bars between trades (default: 15).
	/// </summary>
	public int BaseCooldownBars
	{
		get => _baseCooldownBars.Value;
		set => _baseCooldownBars.Value = value;
	}

	/// <summary>
	/// Use UT Bot trailing stop for exits (default: true).
	/// </summary>
	public bool UseUtExit
	{
		get => _useUtExit.Value;
		set => _useUtExit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public AlmaUtBotConfluenceStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "Length for long-term EMA", "Main");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Main");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 5m)
			.SetDisplay("Stop Loss ATR Mult", "ATR multiplier for stop loss", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 4m)
			.SetDisplay("Take Profit ATR Mult", "ATR multiplier for take profit", "Risk");

		_utKeyValue = Param(nameof(UtKeyValue), 2)
			.SetDisplay("UT Key", "UT Bot key value", "UT Bot");

		_utAtrPeriod = Param(nameof(UtAtrPeriod), 10)
			.SetDisplay("UT ATR Period", "ATR period for UT Bot", "UT Bot");

		_baseCooldownBars = Param(nameof(BaseCooldownBars), 10)
			.SetDisplay("Base Cooldown", "Cooldown in bars between trades", "Filters");

		_useUtExit = Param(nameof(UseUtExit), true)
			.SetDisplay("Use UT Exit", "Use UT Bot trailing stop for exits", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Main");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var atrUt = new AverageTrueRange { Length = UtAtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(new IIndicator[] { ema, atr, atrUt }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaValue = values[0];
		var atrValue = values[1];
		var atrUtValue = values[2];

		var src = candle.ClosePrice;
		var nLoss = UtKeyValue * atrUtValue;

		if (_barIndex == 0)
			_xAtrTrailingStop = src + nLoss;

		if (src > _prevStop && _prevSrc > _prevStop)
			_xAtrTrailingStop = Math.Max(_prevStop, src - nLoss);
		else if (src < _prevStop && _prevSrc < _prevStop)
			_xAtrTrailingStop = Math.Min(_prevStop, src + nLoss);
		else
			_xAtrTrailingStop = src > _prevStop ? src - nLoss : src + nLoss;

		var buyUt = src > _xAtrTrailingStop && _prevSrc <= _prevStop;
		var sellUt = src < _xAtrTrailingStop && _prevSrc >= _prevStop;

		var cooldownOk = _lastTradeIndex is null || _barIndex - _lastTradeIndex >= BaseCooldownBars;

		var buyCondition = buyUt && src > emaValue && cooldownOk;
		var sellCondition = sellUt && src < emaValue && cooldownOk;

		if (buyCondition && Position <= 0)
		{
			BuyMarket();
			_lastTradeIndex = _barIndex;
			_entryPrice = src;
		}
		else if (sellCondition && Position >= 0)
		{
			SellMarket();
			_lastTradeIndex = _barIndex;
			_entryPrice = src;
		}
		else
		{
			ManageExit(candle, atrValue, src);
		}

		_prevSrc = src;
		_prevStop = _xAtrTrailingStop;
		_barIndex++;
	}

	private void ManageExit(ICandleMessage candle, decimal atr, decimal src)
	{
		if (UseUtExit)
		{
			if (Position > 0 && src < _xAtrTrailingStop && _prevSrc >= _prevStop)
			{
				SellMarket();
				_lastTradeIndex = _barIndex;
			}
			else if (Position < 0 && src > _xAtrTrailingStop && _prevSrc <= _prevStop)
			{
				BuyMarket();
				_lastTradeIndex = _barIndex;
			}
		}
		else if (Position != 0)
		{
			var stopLoss = atr * StopLossAtrMultiplier;
			var takeProfit = atr * TakeProfitAtrMultiplier;

			if (Position > 0)
			{
				if (candle.ClosePrice <= _entryPrice - stopLoss || candle.ClosePrice >= _entryPrice + takeProfit)
				{
					SellMarket();
					_lastTradeIndex = _barIndex;
				}
			}
			else
			{
				if (candle.ClosePrice >= _entryPrice + stopLoss || candle.ClosePrice <= _entryPrice - takeProfit)
				{
					BuyMarket();
					_lastTradeIndex = _barIndex;
				}
			}
		}
	}
}

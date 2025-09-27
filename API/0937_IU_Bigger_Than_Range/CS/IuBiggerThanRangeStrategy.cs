using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that enters when the candle body exceeds the previous range.
/// </summary>
public class IuBiggerThanRangeStrategy : Strategy
{
	private enum StopLossMethod
	{
		PreviousHighLow,
		Atr,
		SwingHighLow
	}

	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _riskToReward;
	private readonly StrategyParam<StopLossMethod> _stopLossMethod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _rangeHigh;
	private Lowest _rangeLow;
	private ATR _atr;
	private Highest _swingHigh;
	private Lowest _swingLow;

	private decimal _prevRangeSize;
	private decimal _prevCandleHigh;
	private decimal _prevCandleLow;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Lookback period for range calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public int RiskToReward
	{
		get => _riskToReward.Value;
		set => _riskToReward.Value = value;
	}

	/// <summary>
	/// Stop loss calculation method.
	/// </summary>
	public StopLossMethod StopLoss
	{
		get => _stopLossMethod.Value;
		set => _stopLossMethod.Value = value;
	}

	/// <summary>
	/// ATR indicator length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier factor.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Swing high/low lookback length.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IuBiggerThanRangeStrategy"/> class.
	/// </summary>
	public IuBiggerThanRangeStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Length for range calculation", "Parameters")
			.SetCanOptimize(true);

		_riskToReward = Param(nameof(RiskToReward), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk To Reward", "Risk to reward ratio", "Parameters")
			.SetCanOptimize(true);

		_stopLossMethod = Param(nameof(StopLoss), StopLossMethod.PreviousHighLow)
			.SetDisplay("Stop Loss Method", "Stop loss calculation method", "Risk Management");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR indicator length", "Risk Management")
			.SetCanOptimize(true);

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR multiplier", "Risk Management")
			.SetCanOptimize(true);

		_swingLength = Param(nameof(SwingLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Swing high/low lookback", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevRangeSize = 0m;
		_prevCandleHigh = 0m;
		_prevCandleLow = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_entryPrice = 0m;
		_isLong = false;
		_rangeHigh = null;
		_rangeLow = null;
		_atr = null;
		_swingHigh = null;
		_swingLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rangeHigh = new Highest { Length = LookbackPeriod };
		_rangeLow = new Lowest { Length = LookbackPeriod };
		_atr = new ATR { Length = AtrLength };
		_swingHigh = new Highest { Length = SwingLength };
		_swingLow = new Lowest { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxOc = Math.Max(candle.ClosePrice, candle.OpenPrice);
		var minOc = Math.Min(candle.ClosePrice, candle.OpenPrice);
		var highest = _rangeHigh.Process(maxOc).ToDecimal();
		var lowest = _rangeLow.Process(minOc).ToDecimal();
		var rangeSize = highest - lowest;
		var candleRange = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var atrValue = _atr.Process(candle).ToDecimal();
		var swingHigh = _swingHigh.Process(candle.HighPrice).ToDecimal();
		var swingLow = _swingLow.Process(candle.LowPrice).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRangeSize = rangeSize;
			_prevCandleHigh = candle.HighPrice;
			_prevCandleLow = candle.LowPrice;
			return;
		}

		var cond = candleRange > _prevRangeSize;

		if (cond && Position == 0)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_isLong = true;
				_stopPrice = GetStopPrice(true, atrValue, swingHigh, swingLow);
				_targetPrice = (_entryPrice - _stopPrice) * RiskToReward + _entryPrice;
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_isLong = false;
				_stopPrice = GetStopPrice(false, atrValue, swingHigh, swingLow);
				_targetPrice = _entryPrice - (_stopPrice - _entryPrice) * RiskToReward;
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
			else if (candle.HighPrice >= _targetPrice || candle.ClosePrice >= _targetPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.ClosePrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
			else if (candle.LowPrice <= _targetPrice || candle.ClosePrice <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}

		_prevRangeSize = rangeSize;
		_prevCandleHigh = candle.HighPrice;
		_prevCandleLow = candle.LowPrice;
	}

	private decimal GetStopPrice(bool isLong, decimal atrValue, decimal swingHigh, decimal swingLow)
	{
		return StopLoss switch
		{
			StopLossMethod.PreviousHighLow => isLong ? _prevCandleLow : _prevCandleHigh,
			StopLossMethod.Atr => isLong ? _entryPrice - atrValue * AtrFactor : _entryPrice + atrValue * AtrFactor,
			_ => isLong ? swingLow : swingHigh,
		};
	}

	private void ResetTradeState()
	{
		_stopPrice = 0m;
		_targetPrice = 0m;
		_entryPrice = 0m;
	}
}

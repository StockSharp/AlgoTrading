using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// Strategy based on price crossing EMA channels. Entries use crossovers of close with high/low EMAs. Exits apply stop-loss at previous candle extreme and take-profit based on risk-to-reward ratio.
public class IuEmaChannelStrategy : Strategy
{
	private StrategyParam<int> _emaLength;
	private StrategyParam<decimal> _riskToReward;
	private StrategyParam<int> _maxEntries;
	private StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevHighEma;
	private decimal _prevLowEma;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _isInitialized;
	private int _entriesExecuted;
	private bool _entryPending;
	private bool _exitPending;

	public IuEmaChannelStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100)
			.SetDisplay("EMA Length", "EMA period", "General")
			.SetGreaterThanZero()
			;

		_riskToReward = Param(nameof(RiskToReward), 2m)
			.SetDisplay("Risk To Reward", "Reward to risk ratio", "General")
			.SetGreaterThanZero()
			;

		_maxEntries = Param(nameof(MaxEntries), 35)
			.SetDisplay("Max Entries", "Maximum number of entries per test run", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal RiskToReward { get => _riskToReward.Value; set => _riskToReward.Value = value; }
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isInitialized = false;
		_prevClose = 0m;
		_prevHighEma = 0m;
		_prevLowEma = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_entriesExecuted = 0;
		_entryPending = false;
		_exitPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highEma = new EMA
		{
			Length = EmaLength
		};

		var lowEma = new EMA
		{
			Length = EmaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highEma, lowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highEma);
			DrawIndicator(area, lowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highEmaValue, decimal lowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevHighEma = highEmaValue;
			_prevLowEma = lowEmaValue;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isInitialized = true;
			return;
		}

		if (Position == 0)
		{
			_exitPending = false;
			_entryPending = false;
		}
		else
		{
			_entryPending = false;
		}

		if (Position == 0)
		{
			if (_entriesExecuted >= MaxEntries || _entryPending)
			{
				_prevClose = candle.ClosePrice;
				_prevHighEma = highEmaValue;
				_prevLowEma = lowEmaValue;
				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
				return;
			}

			var crossUp = _prevClose <= _prevHighEma && candle.ClosePrice > highEmaValue;
			var crossDown = _prevClose >= _prevLowEma && candle.ClosePrice < lowEmaValue;

			if (crossUp)
			{
				_stopPrice = _prevLow;
				_takePrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskToReward;
				BuyMarket();
				_entriesExecuted++;
				_entryPending = true;
			}
			else if (crossDown)
			{
				_stopPrice = _prevHigh;
				_takePrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RiskToReward;
				SellMarket();
				_entriesExecuted++;
				_entryPending = true;
			}
		}
		else if (Position > 0)
		{
			if (!_exitPending && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
			{
				SellMarket();
				_exitPending = true;
			}
		}
		else if (Position < 0)
		{
			if (!_exitPending && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
			{
				BuyMarket();
				_exitPending = true;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevHighEma = highEmaValue;
		_prevLowEma = lowEmaValue;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}

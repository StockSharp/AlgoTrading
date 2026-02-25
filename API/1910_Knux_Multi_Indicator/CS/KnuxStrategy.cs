using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Knux multi-indicator strategy.
/// Combines CCI, Williams %R and a moving average crossover for entry signals.
/// </summary>
public class KnuxStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slowMa;
	private CommodityChannelIndex _cci;
	private WilliamsR _wpr;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KnuxStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of fast moving average", "General");

		_slowMaLength = Param(nameof(SlowMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of slow moving average", "General");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "General");

		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "General");

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
		_prevFast = _prevSlow = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var slowResult = _slowMa.Process(candle.ClosePrice, candle.OpenTime, true);
		var cciResult = _cci.Process(candle);
		var wprResult = _wpr.Process(candle);

		if (!slowResult.IsFormed)
		{
			_prevFast = fast;
			return;
		}

		var slow = slowResult.ToDecimal();

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		// If CCI/WPR are formed, use them as filters; otherwise just use MA crossover
		if (cciResult.IsFormed && wprResult.IsFormed)
		{
			var cciVal = cciResult.ToDecimal();
			var wprVal = wprResult.ToDecimal();

			// Buy: MA cross up + CCI negative + WPR oversold
			if (crossUp && cciVal < 0 && wprVal < -50m && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell: MA cross down + CCI positive + WPR overbought
			else if (crossDown && cciVal > 0 && wprVal > -50m && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}
		else
		{
			// Fallback without CCI/WPR
			if (crossUp && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

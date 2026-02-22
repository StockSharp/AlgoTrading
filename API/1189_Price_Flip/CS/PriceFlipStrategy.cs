using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PriceFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _tickerMaxLookback;
	private readonly StrategyParam<int> _tickerMinLookback;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _tickerMax = null!;
	private Lowest _tickerMin = null!;
	private SMA _fastMa = null!;
	private SMA _slowMa = null!;

	private decimal _prevFastMa;
	private decimal _prevSlowMa;
	private decimal _prevClose;

	public int TickerMaxLookback
	{
		get => _tickerMaxLookback.Value;
		set => _tickerMaxLookback.Value = value;
	}

	public int TickerMinLookback
	{
		get => _tickerMinLookback.Value;
		set => _tickerMinLookback.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PriceFlipStrategy()
	{
		_tickerMaxLookback = Param(nameof(TickerMaxLookback), 20)
			.SetGreaterThanZero();

		_tickerMinLookback = Param(nameof(TickerMinLookback), 20)
			.SetGreaterThanZero();

		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero();

		_slowMaLength = Param(nameof(SlowMaLength), 14)
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tickerMax = new Highest { Length = TickerMaxLookback };
		_tickerMin = new Lowest { Length = TickerMinLookback };
		_fastMa = new SMA { Length = FastMaLength };
		_slowMa = new SMA { Length = SlowMaLength };

		_prevFastMa = 0;
		_prevSlowMa = 0;
		_prevClose = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxVal = _tickerMax.Process(candle.HighPrice).ToDecimal();
		var minVal = _tickerMin.Process(candle.LowPrice).ToDecimal();

		if (!_tickerMax.IsFormed || !_tickerMin.IsFormed || !_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			_prevClose = candle.ClosePrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			_prevClose = candle.ClosePrice;
			return;
		}

		var invertedPrice = maxVal + minVal - candle.ClosePrice;

		var bullishCross = _prevFastMa <= _prevSlowMa && fastMaValue > slowMaValue;
		var bearishCross = _prevFastMa >= _prevSlowMa && fastMaValue < slowMaValue;

		if (_prevClose > invertedPrice && bullishCross && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevClose < invertedPrice && bearishCross && Position >= 0)
		{
			SellMarket();
		}

		_prevFastMa = fastMaValue;
		_prevSlowMa = slowMaValue;
		_prevClose = candle.ClosePrice;
	}
}

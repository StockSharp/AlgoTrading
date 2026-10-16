using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades crossings of the upper/lower Bollinger bands and closes on a reverse crossing
/// back inside the broken band. Band values use completed closes, including the signal bar.
/// </summary>
public class FancyBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _closes = new();
	private decimal? _previousClose;
	private decimal? _previousUpper;
	private decimal? _previousLower;
	private bool _entryRequested;
	private bool _exitRequested;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal DeviationMultiplier { get => _deviationMultiplier.Value; set => _deviationMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FancyBollingerBandsStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of completed closes in each band calculation.", "Bollinger");
		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for both bands.", "Bollinger");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_previousClose = null;
		_previousUpper = null;
		_previousLower = null;
		_entryRequested = false;
		_exitRequested = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		_closes.Enqueue(candle.ClosePrice);
		while (_closes.Count > BollingerPeriod)
			_closes.Dequeue();
		if (_closes.Count < BollingerPeriod)
			return;

		var mean = _closes.Average();
		var variance = _closes.Sum(price => (price - mean) * (price - mean)) / BollingerPeriod;
		var deviation = (decimal)Math.Sqrt((double)variance) * DeviationMultiplier;
		var upper = mean + deviation;
		var lower = mean - deviation;
		var close = candle.ClosePrice;

		if (_previousClose is decimal previous && _previousUpper is decimal previousUpper
			&& _previousLower is decimal previousLower)
		{
			if (Position != 0m)
			{
				_entryRequested = false;
				if (!_exitRequested)
				{
					if (Position > 0m && previous >= previousUpper && close <= upper)
					{
						_exitRequested = true;
						SellMarket(Position);
					}
					else if (Position < 0m && previous <= previousLower && close >= lower)
					{
						_exitRequested = true;
						BuyMarket(Math.Abs(Position));
					}
				}
			}
			else
			{
				_exitRequested = false;
				if (!_entryRequested)
				{
					if (previous <= previousUpper && close > upper)
					{
						_entryRequested = true;
						BuyMarket(Volume);
					}
					else if (previous >= previousLower && close < lower)
					{
						_entryRequested = true;
						SellMarket(Volume);
					}
				}
			}
		}

		_previousClose = close;
		_previousUpper = upper;
		_previousLower = lower;
	}
}

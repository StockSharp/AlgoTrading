using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ZScore2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _haEmaLength;
	private readonly StrategyParam<int> _scoreLength;
	private readonly StrategyParam<int> _scoreEmaLength;
	private readonly StrategyParam<int> _rangeWindow;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevScore;
	private decimal _prevEmaScore;
	private decimal _prevLowest;
	private decimal _prevMiddle;
	private decimal _prevHighest;

	public ZScore2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_haEmaLength = Param(nameof(HaEmaLength), 10)
			.SetDisplay("Heikin-Ashi EMA Length", "EMA period for HA close", "Indicators");

		_scoreLength = Param(nameof(ScoreLength), 25)
			.SetDisplay("Score Length", "Lookback for z-score", "Indicators");

		_scoreEmaLength = Param(nameof(ScoreEmaLength), 20)
			.SetDisplay("Score EMA Length", "EMA of score", "Indicators");

		_rangeWindow = Param(nameof(RangeWindow), 100)
			.SetDisplay("Range Window", "Window for highest/lowest", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int HaEmaLength
	{
		get => _haEmaLength.Value;
		set => _haEmaLength.Value = value;
	}

	public int ScoreLength
	{
		get => _scoreLength.Value;
		set => _scoreLength.Value = value;
	}

	public int ScoreEmaLength
	{
		get => _scoreEmaLength.Value;
		set => _scoreEmaLength.Value = value;
	}

	public int RangeWindow
	{
		get => _rangeWindow.Value;
		set => _rangeWindow.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHaOpen = 0m;
		_prevHaClose = 0m;
		_prevScore = 0m;
		_prevEmaScore = 0m;
		_prevLowest = 0m;
		_prevMiddle = 0m;
		_prevHighest = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var haEma = new ExponentialMovingAverage { Length = HaEmaLength };
		var scoreSma = new SimpleMovingAverage { Length = ScoreLength };
		var scoreSd = new StandardDeviation { Length = ScoreLength };
		var scoreEma = new ExponentialMovingAverage { Length = ScoreEmaLength };
		var highest = new Highest { Length = RangeWindow };
		var lowest = new Lowest { Length = RangeWindow };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind((candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				decimal haClose;
				if (_prevHaOpen == 0m)
				{
					var haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
					haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;
					_prevHaOpen = haOpen;
					_prevHaClose = haClose;
				}
				else
				{
					var haOpen = (_prevHaOpen + _prevHaClose) / 2m;
					haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;
					_prevHaOpen = haOpen;
					_prevHaClose = haClose;
				}

				var emaHa = haEma.Process(haClose, candle.ServerTime, true).ToDecimal();
				var mean = scoreSma.Process(emaHa, candle.ServerTime, true).ToDecimal();
				var sd = scoreSd.Process(emaHa, candle.ServerTime, true).ToDecimal();
				var score = sd == 0m ? 0m : (emaHa - mean) / sd;
				var emaScoreVal = scoreEma.Process(score, candle.ServerTime, true).ToDecimal();
				var high = highest.Process(emaScoreVal, candle.ServerTime, true).ToDecimal();
				var low = lowest.Process(emaScoreVal, candle.ServerTime, true).ToDecimal();
				var middle = (high + low) / 2m;

				if (!haEma.IsFormed || !scoreSma.IsFormed || !scoreSd.IsFormed || !highest.IsFormed || !lowest.IsFormed)
				{
					_prevScore = score;
					_prevEmaScore = emaScoreVal;
					_prevLowest = low;
					_prevMiddle = middle;
					_prevHighest = high;
					return;
				}

				var longCon = (_prevScore <= low && score > low) || (_prevEmaScore <= middle && emaScoreVal > middle);
				var addOn = _prevScore <= high && score > high;
				var shortCon = (_prevEmaScore >= high && emaScoreVal < high) || (_prevEmaScore >= low && emaScoreVal < low);

				if (longCon && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (addOn && Position > 0)
				{
					BuyMarket(Volume);
				}
				else if (shortCon && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
				}

				_prevScore = score;
				_prevEmaScore = emaScoreVal;
				_prevLowest = low;
				_prevMiddle = middle;
				_prevHighest = high;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, scoreEma);
			DrawOwnTrades(area);
		}
	}
}

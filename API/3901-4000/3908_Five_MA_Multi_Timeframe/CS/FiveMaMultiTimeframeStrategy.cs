using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Five moving averages across three timeframes with Accelerator Oscillator confirmation.
/// </summary>
public class FiveMaMultiTimeframeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe1;
	private readonly StrategyParam<DataType> _higherTimeframe2;
	private readonly StrategyParam<int> _firstPeriod;
	private readonly StrategyParam<int> _secondPeriod;
	private readonly StrategyParam<int> _thirdPeriod;
	private readonly StrategyParam<int> _fourthPeriod;
	private readonly StrategyParam<int> _fifthPeriod;
	private readonly StrategyParam<int> _openLevel;
	private readonly StrategyParam<int> _closeLevel;

	private readonly Dictionary<string, FrameState> _frames = new(StringComparer.Ordinal);

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType HigherTimeframe1 { get => _higherTimeframe1.Value; set => _higherTimeframe1.Value = value; }
	public DataType HigherTimeframe2 { get => _higherTimeframe2.Value; set => _higherTimeframe2.Value = value; }
	public int FirstPeriod { get => _firstPeriod.Value; set => _firstPeriod.Value = value; }
	public int SecondPeriod { get => _secondPeriod.Value; set => _secondPeriod.Value = value; }
	public int ThirdPeriod { get => _thirdPeriod.Value; set => _thirdPeriod.Value = value; }
	public int FourthPeriod { get => _fourthPeriod.Value; set => _fourthPeriod.Value = value; }
	public int FifthPeriod { get => _fifthPeriod.Value; set => _fifthPeriod.Value = value; }
	public int OpenLevel { get => _openLevel.Value; set => _openLevel.Value = value; }
	public int CloseLevel { get => _closeLevel.Value; set => _closeLevel.Value = value; }

	public FiveMaMultiTimeframeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Primary signal timeframe.", "Timeframes");
		_higherTimeframe1 = Param(nameof(HigherTimeframe1), TimeSpan.FromMinutes(60).TimeFrame()).SetDisplay("Higher Timeframe 1", "First confirmation timeframe.", "Timeframes");
		_higherTimeframe2 = Param(nameof(HigherTimeframe2), TimeSpan.FromMinutes(240).TimeFrame()).SetDisplay("Higher Timeframe 2", "Slow trend timeframe.", "Timeframes");

		_firstPeriod = Param(nameof(FirstPeriod), 5).SetGreaterThanZero();
		_secondPeriod = Param(nameof(SecondPeriod), 8).SetGreaterThanZero();
		_thirdPeriod = Param(nameof(ThirdPeriod), 13).SetGreaterThanZero();
		_fourthPeriod = Param(nameof(FourthPeriod), 21).SetGreaterThanZero();
		_fifthPeriod = Param(nameof(FifthPeriod), 34).SetGreaterThanZero();

		_openLevel = Param(nameof(OpenLevel), 0).SetNotNegative().SetDisplay("Open Level", "Minimum signal grade required to open.", "Signal");
		_closeLevel = Param(nameof(CloseLevel), 1).SetNotNegative().SetDisplay("Close Level", "Opposite signal grade required to close.", "Signal");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, HigherTimeframe1), (Security, HigherTimeframe2)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_frames.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartFrame("primary", CandleType);
		StartFrame("higher1", HigherTimeframe1);
		StartFrame("higher2", HigherTimeframe2);
	}

	private void StartFrame(string key, DataType candleType)
	{
		var frame = new FrameState([FirstPeriod, SecondPeriod, ThirdPeriod, FourthPeriod, FifthPeriod]);
		_frames[key] = frame;

		var subscription = SubscribeCandles(candleType);
		subscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			frame.Process(candle);
			Evaluate();
		}).Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void Evaluate()
	{
		if (_frames.Count != 3 || _frames.Values.Any(f => !f.IsReady))
			return;

		var bullishGrade = _frames.Values.Min(f => Grade(f.BullishScore));
		var bearishGrade = _frames.Values.Min(f => Grade(f.BearishScore));

		if (Position > 0)
		{
			if (bearishGrade >= CloseLevel)
				SellMarket(Math.Abs(Position));
			return;
		}

		if (Position < 0)
		{
			if (bullishGrade >= CloseLevel)
				BuyMarket(Math.Abs(Position));
			return;
		}

		if (bullishGrade > OpenLevel && bullishGrade > bearishGrade)
			BuyMarket();
		else if (bearishGrade > OpenLevel && bearishGrade > bullishGrade)
			SellMarket();
	}

	private static int Grade(decimal score)
		=> score > 75m ? 2 : score > 50m ? 1 : 0;

	private sealed class FrameState(int[] periods)
	{
		private readonly int[] _periods = periods;
		private readonly List<decimal> _closes = [];
		private readonly List<decimal> _medians = [];
		private readonly List<decimal> _ao = [];
		private readonly List<decimal> _ac = [];
		private decimal[] _previousMas;

		public decimal BullishScore { get; private set; }
		public decimal BearishScore { get; private set; }
		public bool IsReady { get; private set; }

		public void Process(ICandleMessage candle)
		{
			_closes.Add(candle.ClosePrice);
			_medians.Add((candle.HighPrice + candle.LowPrice) / 2m);

			if (_medians.Count >= 34)
			{
				var ao = AverageTail(_medians, 5) - AverageTail(_medians, 34);
				_ao.Add(ao);

				if (_ao.Count >= 5)
					_ac.Add(ao - AverageTail(_ao, 5));
			}

			if (_closes.Count < _periods.Max())
				return;

			var mas = _periods.Select(p => AverageTail(_closes, p)).ToArray();

			if (_previousMas is null)
			{
				_previousMas = mas;
				return;
			}

			var bullishVotes = 0;
			var bearishVotes = 0;

			for (var i = 0; i < mas.Length; i++)
			{
				if (mas[i] > _previousMas[i])
					bullishVotes++;
				else if (mas[i] < _previousMas[i])
					bearishVotes++;
			}

			if (_ac.Count >= 4)
			{
				var last = _ac.Count - 1;
				var bullishAc = _ac[last - 3] < _ac[last - 2] && _ac[last - 2] < _ac[last - 1] && _ac[last - 1] < _ac[last];
				var bearishAc = _ac[last - 3] > _ac[last - 2] && _ac[last - 2] > _ac[last - 1] && _ac[last - 1] > _ac[last];

				if (bullishAc)
					bullishVotes++;
				else if (bearishAc)
					bearishVotes++;
			}

			BullishScore = bullishVotes / 6m * 100m;
			BearishScore = bearishVotes / 6m * 100m;
			IsReady = true;
			_previousMas = mas;

			var keep = Math.Max(_periods.Max(), 40);
			if (_closes.Count > keep)
				_closes.RemoveRange(0, _closes.Count - keep);
			if (_medians.Count > 40)
				_medians.RemoveRange(0, _medians.Count - 40);
			if (_ao.Count > 10)
				_ao.RemoveRange(0, _ao.Count - 10);
			if (_ac.Count > 10)
				_ac.RemoveRange(0, _ac.Count - 10);
		}

		private static decimal AverageTail(List<decimal> values, int count)
		{
			var start = values.Count - count;
			var sum = 0m;
			for (var i = start; i < values.Count; i++)
				sum += values[i];
			return sum / count;
		}
	}
}

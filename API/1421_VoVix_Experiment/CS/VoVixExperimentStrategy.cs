using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class VoVixExperimentStrategy : Strategy
{
	private readonly StrategyParam<int> _fastAtrLength;
	private readonly StrategyParam<int> _slowAtrLength;
	private readonly StrategyParam<int> _zScoreWindow;
	private readonly StrategyParam<decimal> _entryZ;
	private readonly StrategyParam<decimal> _exitZ;
	private readonly StrategyParam<int> _localMaxWindow;
	private readonly StrategyParam<decimal> _superZ;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal __prevZ;

	public VoVixExperimentStrategy()
	{
		fastAtrLength = Param(nameof(FastAtrLength), 13)
			.SetDisplay("Fast ATR Length", "Period for fast ATR", "Indicators");

		slowAtrLength = Param(nameof(SlowAtrLength), 26)
			.SetDisplay("Slow ATR Length", "Period for slow ATR", "Indicators");

		zScoreWindow = Param(nameof(ZScoreWindow), 81)
			.SetDisplay("Z-Score Window", "Lookback for z-score", "Indicators");

		entryZ = Param(nameof(EntryZ), 1.0m)
			.SetDisplay("Entry Z", "Minimum z-score to enter", "Strategy");

		exitZ = Param(nameof(ExitZ), 1.4m)
			.SetDisplay("Exit Z", "Z-score threshold to exit", "Strategy");

		localMaxWindow = Param(nameof(LocalMaxWindow), 6)
			.SetDisplay("Local Max Window", "Bars for local maximum", "Indicators");

		superZ = Param(nameof(SuperZ), 2.0m)
			.SetDisplay("Super Spike Z", "Z-score for super spike", "Strategy");

		minVolume = Param(nameof(MinVolume), 1m)
			.SetDisplay("Min Volume", "Volume for normal spike", "General");

		maxVolume = Param(nameof(MaxVolume), 2m)
			.SetDisplay("Max Volume", "Volume for super spike", "General");

		candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
	}

	public int FastAtrLength
	{
		get => _fastAtrLength.Value;
		set => _fastAtrLength.Value = value;
	}

	public int SlowAtrLength
	{
		get => _slowAtrLength.Value;
		set => _slowAtrLength.Value = value;
	}

	public int ZScoreWindow
	{
		get => _zScoreWindow.Value;
		set => _zScoreWindow.Value = value;
	}

	public decimal EntryZ
	{
		get => _entryZ.Value;
		set => _entryZ.Value = value;
	}

	public decimal ExitZ
	{
		get => _exitZ.Value;
		set => _exitZ.Value = value;
	}

	public int LocalMaxWindow
	{
		get => _localMaxWindow.Value;
		set => _localMaxWindow.Value = value;
	}

	public decimal SuperZ
	{
		get => _superZ.Value;
		set => _superZ.Value = value;
	}

	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevZ = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastAtr = new AverageTrueRange { Length = FastAtrLength };
		var slowAtr = new AverageTrueRange { Length = SlowAtrLength };
		var zMa = new SimpleMovingAverage { Length = ZScoreWindow };
		var zSd = new StandardDeviation { Length = ZScoreWindow };
		var localMax = new Highest { Length = LocalMaxWindow };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastAtr, slowAtr, (candle, fast, slow) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var voVix = slow == 0m ? 0m : fast / slow;
				var maVal = zMa.Process(voVix, candle.ServerTime, true).ToDecimal();
				var sdVal = zSd.Process(voVix, candle.ServerTime, true).ToDecimal();
				var z = sdVal == 0m ? 0m : (voVix - maVal) / sdVal;
				var maxVal = localMax.Process(voVix, candle.ServerTime, true).ToDecimal();

				if (!zMa.IsFormed || !zSd.IsFormed || !localMax.IsFormed)
				{
					_prevZ = z;
					return;
				}

				var isSpike = z > EntryZ && voVix >= maxVal;
				var isSuper = z > SuperZ;
				var exit = z < ExitZ;

				var volume = isSuper ? MaxVolume : MinVolume;

				if (isSpike && candle.ClosePrice > candle.OpenPrice && Position <= 0)
				{
					BuyMarket(volume + Math.Abs(Position));
				}
				else if (isSpike && candle.ClosePrice < candle.OpenPrice && Position >= 0)
				{
					SellMarket(volume + Math.Abs(Position));
				}
				else if (exit && Position > 0)
				{
					SellMarket(Math.Abs(Position));
				}
				else if (exit && Position < 0)
				{
					BuyMarket(Math.Abs(Position));
				}

				_prevZ = z;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastAtr);
			DrawIndicator(area, slowAtr);
			DrawOwnTrades(area);
		}
	}
}

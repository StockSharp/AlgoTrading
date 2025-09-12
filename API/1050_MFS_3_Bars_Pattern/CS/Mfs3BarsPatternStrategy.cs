using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MFS 3 Bars Pattern strategy.
/// Looks for an ignite bar followed by a pullback and confirmation bar in a downtrend.
/// Enters long on confirmation with stop-loss at ignite low and risk-reward based target.
/// </summary>
public class Mfs3BarsPatternStrategy : Strategy
{
	private readonly StrategyParam<int> _smaShortLength;
	private readonly StrategyParam<int> _smaMedLength;
	private readonly StrategyParam<int> _smaLongLength;
	private readonly StrategyParam<decimal> _igniteMultiplier;
	private readonly StrategyParam<decimal> _maxPullbackSize;
	private readonly StrategyParam<decimal> _minConfirmationSize;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<CandleInfo> _lastThreeCandles = new(3);
	private SMA _bodySma;

	/// <summary>
	/// Short SMA period.
	/// </summary>
	public int SmaShortLength
	{
		get => _smaShortLength.Value;
		set => _smaShortLength.Value = value;
	}

	/// <summary>
	/// Medium SMA period.
	/// </summary>
	public int SmaMedLength
	{
		get => _smaMedLength.Value;
		set => _smaMedLength.Value = value;
	}

	/// <summary>
	/// Long SMA period.
	/// </summary>
	public int SmaLongLength
	{
		get => _smaLongLength.Value;
		set => _smaLongLength.Value = value;
	}

	/// <summary>
	/// Ignite multiplier for average body size.
	/// </summary>
	public decimal IgniteMultiplier
	{
		get => _igniteMultiplier.Value;
		set => _igniteMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum pullback body size relative to ignite bar.
	/// </summary>
	public decimal MaxPullbackSize
	{
		get => _maxPullbackSize.Value;
		set => _maxPullbackSize.Value = value;
	}

	/// <summary>
	/// Minimum confirmation body size relative to ignite bar.
	/// </summary>
	public decimal MinConfirmationSize
	{
		get => _minConfirmationSize.Value;
		set => _minConfirmationSize.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Mfs3BarsPatternStrategy()
	{
		_smaShortLength = Param(nameof(SmaShortLength), 20)
			.SetDisplay("SMA Short", "Period for short moving average", "SMA")
			.SetCanOptimize(true);

		_smaMedLength = Param(nameof(SmaMedLength), 50)
			.SetDisplay("SMA Medium", "Period for medium moving average", "SMA")
			.SetCanOptimize(true);

		_smaLongLength = Param(nameof(SmaLongLength), 200)
			.SetDisplay("SMA Long", "Period for long moving average", "SMA")
			.SetCanOptimize(true);

		_igniteMultiplier = Param(nameof(IgniteMultiplier), 3m)
			.SetDisplay("Ignite Multiplier", "Multiplier for average body size", "Pattern");

		_maxPullbackSize = Param(nameof(MaxPullbackSize), 0.33m)
			.SetDisplay("Max Pullback Size", "Max pullback body size relative to ignite bar", "Pattern");

		_minConfirmationSize = Param(nameof(MinConfirmationSize), 0.33m)
			.SetDisplay("Min Confirmation Size", "Min confirmation body size relative to ignite bar", "Pattern");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_lastThreeCandles.Clear();
		_bodySma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var smaShort = new SMA { Length = SmaShortLength };
		var smaMed = new SMA { Length = SmaMedLength };
		var smaLong = new SMA { Length = SmaLongLength };
		_bodySma = new SMA { Length = SmaMedLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(smaShort, smaMed, smaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, smaShort);
			DrawIndicator(area, smaMed);
			DrawIndicator(area, smaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaShortValue, decimal smaMedValue, decimal smaLongValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var avgBody = _bodySma.Process(body, candle.ServerTime, true).ToDecimal();
		var isIgnite = _bodySma.IsFormed && body >= avgBody * IgniteMultiplier && candle.ClosePrice > candle.OpenPrice;

		_lastThreeCandles.Enqueue(new CandleInfo(candle, body, avgBody, smaShortValue, smaMedValue, smaLongValue, isIgnite));
		while (_lastThreeCandles.Count > 3)
			_lastThreeCandles.Dequeue();

		if (_lastThreeCandles.Count < 3)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candles = _lastThreeCandles.ToArray();
		var ignite = candles[0];
		var pullback = candles[1];
		var confirm = candles[2];

		var pullbackOk = pullback.Candle.ClosePrice < pullback.Candle.OpenPrice &&
			ignite.IsIgnite &&
			pullback.Body < ignite.Body * MaxPullbackSize &&
			pullback.Candle.HighPrice > ignite.Candle.ClosePrice &&
			pullback.Candle.LowPrice < ignite.Candle.ClosePrice;

		var confirmationOk = confirm.Candle.ClosePrice > confirm.Candle.OpenPrice &&
			pullbackOk &&
			confirm.Body > ignite.Body * MinConfirmationSize &&
			confirm.Candle.OpenPrice > pullback.Candle.ClosePrice;

		var trendOk = ignite.SmaLong > ignite.SmaMed && ignite.SmaMed > ignite.SmaShort &&
			ignite.Candle.ClosePrice < ignite.SmaShort;

		var signal = ignite.IsIgnite && pullbackOk && confirmationOk && trendOk;

		if (signal && Position <= 0)
		{
			var stopPrice = ignite.Candle.LowPrice;
			var targetPrice = confirm.Candle.OpenPrice + (confirm.Candle.OpenPrice - stopPrice) * RiskReward;
			var volume = Volume + Math.Abs(Position);

			BuyMarket(volume);
			SellStop(volume, stopPrice);
			SellLimit(volume, targetPrice);
		}
	}

	private record CandleInfo(ICandleMessage Candle, decimal Body, decimal AvgBody, decimal SmaShort, decimal SmaMed, decimal SmaLong, bool IsIgnite);
}

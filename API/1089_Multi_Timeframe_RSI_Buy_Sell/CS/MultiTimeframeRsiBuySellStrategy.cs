using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi timeframe RSI strategy. Generates buy signal when all enabled RSI values are below BuyThreshold.
/// Generates sell signal when all enabled RSI values are above SellThreshold.
/// </summary>
public class MultiTimeframeRsiBuySellStrategy : Strategy
{
	private readonly StrategyParam<bool> _rsi1Enabled;
	private readonly StrategyParam<bool> _rsi2Enabled;
	private readonly StrategyParam<bool> _rsi3Enabled;
	private readonly StrategyParam<int> _rsi1Length;
	private readonly StrategyParam<int> _rsi2Length;
	private readonly StrategyParam<int> _rsi3Length;
	private readonly StrategyParam<DataType> _rsi1CandleType;
	private readonly StrategyParam<DataType> _rsi2CandleType;
	private readonly StrategyParam<DataType> _rsi3CandleType;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<int> _cooldownPeriod;

	private decimal? _rsi1;
	private decimal? _rsi2;
	private decimal? _rsi3;
	private int _buyCooldown;
	private int _sellCooldown;

	/// <summary>
	/// First RSI enabled.
	/// </summary>
	public bool Rsi1Enabled { get => _rsi1Enabled.Value; set => _rsi1Enabled.Value = value; }

	/// <summary>
	/// Second RSI enabled.
	/// </summary>
	public bool Rsi2Enabled { get => _rsi2Enabled.Value; set => _rsi2Enabled.Value = value; }

	/// <summary>
	/// Third RSI enabled.
	/// </summary>
	public bool Rsi3Enabled { get => _rsi3Enabled.Value; set => _rsi3Enabled.Value = value; }

	/// <summary>
	/// Period for first RSI.
	/// </summary>
	public int Rsi1Length { get => _rsi1Length.Value; set => _rsi1Length.Value = value; }

	/// <summary>
	/// Period for second RSI.
	/// </summary>
	public int Rsi2Length { get => _rsi2Length.Value; set => _rsi2Length.Value = value; }

	/// <summary>
	/// Period for third RSI.
	/// </summary>
	public int Rsi3Length { get => _rsi3Length.Value; set => _rsi3Length.Value = value; }

	/// <summary>
	/// Timeframe for first RSI.
	/// </summary>
	public DataType Rsi1CandleType { get => _rsi1CandleType.Value; set => _rsi1CandleType.Value = value; }

	/// <summary>
	/// Timeframe for second RSI.
	/// </summary>
	public DataType Rsi2CandleType { get => _rsi2CandleType.Value; set => _rsi2CandleType.Value = value; }

	/// <summary>
	/// Timeframe for third RSI.
	/// </summary>
	public DataType Rsi3CandleType { get => _rsi3CandleType.Value; set => _rsi3CandleType.Value = value; }

	/// <summary>
	/// RSI level to trigger buy.
	/// </summary>
	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }

	/// <summary>
	/// RSI level to trigger sell.
	/// </summary>
	public decimal SellThreshold { get => _sellThreshold.Value; set => _sellThreshold.Value = value; }

	/// <summary>
	/// Cooldown period in bars.
	/// </summary>
	public int CooldownPeriod { get => _cooldownPeriod.Value; set => _cooldownPeriod.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiTimeframeRsiBuySellStrategy"/>.
	/// </summary>
	public MultiTimeframeRsiBuySellStrategy()
	{
		_rsi1Enabled = Param(nameof(Rsi1Enabled), true)
		.SetDisplay("RSI1 Enabled", "Use first RSI", "RSI1");
		_rsi2Enabled = Param(nameof(Rsi2Enabled), true)
		.SetDisplay("RSI2 Enabled", "Use second RSI", "RSI2");
		_rsi3Enabled = Param(nameof(Rsi3Enabled), true)
		.SetDisplay("RSI3 Enabled", "Use third RSI", "RSI3");

		_rsi1Length = Param(nameof(Rsi1Length), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI1 Length", "Period for first RSI", "RSI1");
		_rsi2Length = Param(nameof(Rsi2Length), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI2 Length", "Period for second RSI", "RSI2");
		_rsi3Length = Param(nameof(Rsi3Length), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI3 Length", "Period for third RSI", "RSI3");

		_rsi1CandleType = Param(nameof(Rsi1CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("RSI1 Timeframe", "Timeframe for first RSI", "RSI1");
		_rsi2CandleType = Param(nameof(Rsi2CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("RSI2 Timeframe", "Timeframe for second RSI", "RSI2");
		_rsi3CandleType = Param(nameof(Rsi3CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("RSI3 Timeframe", "Timeframe for third RSI", "RSI3");

		_buyThreshold = Param(nameof(BuyThreshold), 30m)
		.SetRange(0m, 100m)
		.SetDisplay("Buy Threshold", "RSI level to enter long", "Strategy");
		_sellThreshold = Param(nameof(SellThreshold), 70m)
		.SetRange(0m, 100m)
		.SetDisplay("Sell Threshold", "RSI level to enter short", "Strategy");
		_cooldownPeriod = Param(nameof(CooldownPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Cooldown", "Bars to wait between signals", "Strategy");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, Rsi1CandleType), (Security, Rsi2CandleType), (Security, Rsi3CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi1 = _rsi2 = _rsi3 = null;
		_buyCooldown = _sellCooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_buyCooldown = _sellCooldown = CooldownPeriod;

		if (Rsi1Enabled)
		{
			var rsi1 = new RelativeStrengthIndex { Length = Rsi1Length };
			var sub1 = SubscribeCandles(Rsi1CandleType);
			sub1.Bind(rsi1, ProcessRsi1).Start();
		}

		if (Rsi2Enabled)
		{
			var rsi2 = new RelativeStrengthIndex { Length = Rsi2Length };
			var sub2 = SubscribeCandles(Rsi2CandleType);
			sub2.Bind(rsi2, ProcessRsi2).Start();
		}

		if (Rsi3Enabled)
		{
			var rsi3 = new RelativeStrengthIndex { Length = Rsi3Length };
			var sub3 = SubscribeCandles(Rsi3CandleType);
			sub3.Bind(rsi3, ProcessRsi3).Start();
		}
	}

	private void ProcessRsi1(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsi1 = value;
		TryTrade(candle);
	}

	private void ProcessRsi2(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsi2 = value;
	}

	private void ProcessRsi3(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsi3 = value;
	}

	private void TryTrade(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if ((Rsi1Enabled && _rsi1 == null) || (Rsi2Enabled && _rsi2 == null) || (Rsi3Enabled && _rsi3 == null))
		return;

		if (_buyCooldown < CooldownPeriod)
		_buyCooldown++;
		if (_sellCooldown < CooldownPeriod)
		_sellCooldown++;

		var buyOk = (!Rsi1Enabled || _rsi1 < BuyThreshold) && (!Rsi2Enabled || _rsi2 < BuyThreshold) && (!Rsi3Enabled || _rsi3 < BuyThreshold);
		var sellOk = (!Rsi1Enabled || _rsi1 > SellThreshold) && (!Rsi2Enabled || _rsi2 > SellThreshold) && (!Rsi3Enabled || _rsi3 > SellThreshold);

		if (buyOk && _buyCooldown >= CooldownPeriod)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_sellCooldown = CooldownPeriod;
			_buyCooldown = 0;
		}
		else if (sellOk && _sellCooldown >= CooldownPeriod)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_sellCooldown = 0;
			_buyCooldown = CooldownPeriod;
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Real-time currency power monitor for FORTS futures basket.
/// </summary>
public class FortsCurrencyPowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<Security> _mixSecurity;
	private readonly StrategyParam<Security> _rtsSecurity;
	private readonly StrategyParam<Security> _siSecurity;
	private readonly StrategyParam<Security> _euSecurity;

	private readonly Dictionary<Security, SecurityContext> _securityContexts = new();
	private Basket[] _baskets = Array.Empty<Basket>();

	/// <summary>
	/// Candles used to compute basket powers.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback for Donchian channel that measures recent range.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// MIX contract participating in RTS and RUB baskets.
	/// </summary>
	public Security MixSecurity
	{
		get => _mixSecurity.Value;
		set => _mixSecurity.Value = value;
	}

	/// <summary>
	/// RTS futures used across two baskets.
	/// </summary>
	public Security RtsSecurity
	{
		get => _rtsSecurity.Value;
		set => _rtsSecurity.Value = value;
	}

	/// <summary>
	/// SI futures representing USD against RUB.
	/// </summary>
	public Security SiSecurity
	{
		get => _siSecurity.Value;
		set => _siSecurity.Value = value;
	}

	/// <summary>
	/// EU futures to capture EUR influence within RUB basket.
	/// </summary>
	public Security EuSecurity
	{
		get => _euSecurity.Value;
		set => _euSecurity.Value = value;
	}

	public FortsCurrencyPowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to evaluate", "General");

		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of candles for Donchian range", "Parameters");

		_mixSecurity = Param<Security>(nameof(MixSecurity))
			.SetDisplay("MIX Security", "Equity index future in basket", "Securities")
			.SetRequired();

		_rtsSecurity = Param<Security>(nameof(RtsSecurity))
			.SetDisplay("RTS Security", "RTS index future", "Securities")
			.SetRequired();

		_siSecurity = Param<Security>(nameof(SiSecurity))
			.SetDisplay("Si Security", "USD/RUB future", "Securities")
			.SetRequired();

		_euSecurity = Param<Security>(nameof(EuSecurity))
			.SetDisplay("Eu Security", "EUR/RUB future", "Securities")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var candleType = CandleType;
		var seen = new HashSet<Security>();

		if (Security != null && seen.Add(Security))
			yield return (Security, candleType);

		if (MixSecurity != null && seen.Add(MixSecurity))
			yield return (MixSecurity, candleType);

		if (RtsSecurity != null && seen.Add(RtsSecurity))
			yield return (RtsSecurity, candleType);

		if (SiSecurity != null && seen.Add(SiSecurity))
			yield return (SiSecurity, candleType);

		if (EuSecurity != null && seen.Add(EuSecurity))
			yield return (EuSecurity, candleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_securityContexts.Clear();
		_baskets = Array.Empty<Basket>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (MixSecurity == null || RtsSecurity == null || SiSecurity == null || EuSecurity == null)
			throw new InvalidOperationException("All basket securities must be specified.");

		_securityContexts.Clear();

		var mixContext = CreateContext(MixSecurity);
		var rtsContext = CreateContext(RtsSecurity);
		var siContext = CreateContext(SiSecurity);
		var euContext = CreateContext(EuSecurity);

		_baskets =
		[
			new("RTS", new[] { new BasketComponent(mixContext, false), new BasketComponent(rtsContext, false) }),
			new("USD", new[] { new BasketComponent(siContext, false), new BasketComponent(rtsContext, true) }),
			new("RUB", new[] { new BasketComponent(siContext, true), new BasketComponent(mixContext, true), new BasketComponent(euContext, true) })
		];

		foreach (var context in _securityContexts.Values)
		{
			var subscription = SubscribeCandles(CandleType, security: context.Security);
			subscription
				.BindEx(context.Donchian, (candle, value) => ProcessSecurity(context, candle, value))
				.Start();
		}
	}

	private SecurityContext CreateContext(Security security)
	{
		if (_securityContexts.TryGetValue(security, out var existing))
			return existing;

		var context = new SecurityContext(security, Lookback);
		_securityContexts.Add(security, context);
		return context;
	}

	private void ProcessSecurity(SecurityContext context, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// Ignore updates until the candle is completed.
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		// Donchian channel provides upper and lower extremes for the lookback range.
		var donchianValue = (DonchianChannelsValue)indicatorValue;

		if (donchianValue.UpperBand is not decimal upper || donchianValue.LowerBand is not decimal lower)
			return;

		var range = upper - lower;
		if (range == 0m)
			return;

		var close = candle.ClosePrice;
		var relative = (close - lower) / range * 100m;
		relative = Math.Max(0m, Math.Min(100m, relative));

		// Store the latest normalized power value for downstream aggregation.
		context.Power = relative;
		context.LastUpdate = candle.CloseTime ?? candle.ServerTime;

		UpdateBaskets();
	}

	private void UpdateBaskets()
	{
		foreach (var basket in _baskets)
		{
			// Each basket aggregates the latest powers from its components.
			if (!basket.TryCalculate(out var value, out var time))
				continue;

			if (time <= basket.LastPublished && basket.LastValue.HasValue && basket.LastValue.Value.Equals(value))
				continue;

			basket.LastValue = value;
			basket.LastPublished = time;

			// Report the basket status for visualization or external processing.
			LogInfo($"{basket.Name} basket power = {value:F2} at {time:O}");
		}
	}

	private sealed class SecurityContext
	{
		public SecurityContext(Security security, int lookback)
		{
			Security = security;
			Donchian = new DonchianChannels { Length = lookback };
		}

		public Security Security { get; }
		public DonchianChannels Donchian { get; }
		public decimal? Power { get; set; }
		public DateTimeOffset LastUpdate { get; set; }
	}

	private sealed class BasketComponent
	{
		public BasketComponent(SecurityContext context, bool invert)
		{
			Context = context;
			Invert = invert;
		}

		public SecurityContext Context { get; }
		public bool Invert { get; }
	}

	private sealed class Basket
	{
		public Basket(string name, BasketComponent[] components)
		{
			Name = name;
			Components = components;
			LastPublished = DateTimeOffset.MinValue;
		}

		public string Name { get; }
		public BasketComponent[] Components { get; }
		public decimal? LastValue { get; set; }
		public DateTimeOffset LastPublished { get; set; }

		public bool TryCalculate(out decimal value, out DateTimeOffset timestamp)
		{
			decimal sum = 0m;
			var latest = DateTimeOffset.MinValue;

			foreach (var component in Components)
			{
				var power = component.Context.Power;
				if (power is null)
				{
					value = 0m;
					timestamp = latest;
					return false;
				}

				var adjusted = component.Invert ? 100m - power.Value : power.Value;
				sum += adjusted;

				if (component.Context.LastUpdate > latest)
					latest = component.Context.LastUpdate;
			}

			value = sum / Components.Length;
			timestamp = latest;
			return true;
		}
	}
}

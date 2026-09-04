namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Algo.Candles.Patterns;
using StockSharp.Algo.Indicators;
using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

/// <summary>
/// Loads a gallery file into a composition through the same registry the Designer uses, so a file that
/// loads here is a file the Designer opens.
/// </summary>
public static class SchemaLoader
{
	private static readonly Lock _sync = new();
	private static ICompositionRegistry _registry;

	/// <summary>The registry holding every diagram element the Designer offers.</summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The registry, built on first use.</returns>
	public static async Task<ICompositionRegistry> GetRegistryAsync(CancellationToken cancellationToken)
	{
		ICandlePatternProvider patterns = null;

		using (_sync.EnterScope())
		{
			if (_registry is null)
			{

				// Elements reach for these services while they load; a missing one surfaces as an element
				// that silently fails to build rather than as an error.
				if (ConfigManager.TryGetService<IDispatcher>() is null)
					ConfigManager.RegisterService<IDispatcher>(new DummyDispatcher());

				if (ConfigManager.TryGetService<IIndicatorProvider>() is null)
				{
					var indicators = new IndicatorProvider();
					indicators.Init();
					ConfigManager.RegisterService<IIndicatorProvider>(indicators);
				}

				// A variable holding an instrument resolves it while the element loads: it looks the identifier
				// up, and failing that builds a stand-in, which needs a board to put it on. Without this the
				// lookup ends in an error the element swallows, and every source falls back to the strategy's
				// own instrument -- so a schema on two instruments quietly becomes a schema on one.
				if (ConfigManager.TryGetService<IExchangeInfoProvider>() is null)
					ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());

				// An example built on a candle pattern asks the provider for it while the element loads, and
				// without one the indicator is left unbuilt and the diagram runs on in silence.
				if (ConfigManager.TryGetService<ICandlePatternProvider>() is null)
				{
					patterns = new InMemoryCandlePatternProvider();
					ConfigManager.RegisterService(patterns);
				}

				if (ConfigManager.TryGetService<CompilerProvider>() is null)
					ConfigManager.RegisterService(new CompilerProvider { { FileExts.CSharp, new CSharpCompiler() } });

				var registry = new CompositionRegistry<InMemoryCompositionModelNode, InMemoryCompositionModelLink>(() => new InMemoryCompositionModelBehavior());
				registry.FillDefault();

				// The chart panel a gallery example draws on is provided by the Designer window, so nothing
				// answers for its identifier here and every schema holding one would fail to build. The
				// platform ships a headless stand-in under the same identifier for exactly this case.
				registry.DiagramElements.Add(new DummyChartDiagramElement());

				_registry = registry;
			}
		}

		// The registry itself is built under the lock; filling the pattern provider is awaited outside it.
		if (patterns is not null)
			await patterns.InitAsync(cancellationToken);

		return _registry;
	}

	/// <summary>The identifiers of elements a file expects the reader to already own.</summary>
	/// <param name="fileName">Full path of the schema file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Type identifiers of the nodes that stand for a composite element.</returns>
	public static async Task<IReadOnlyCollection<Guid>> ReadCompositeTypesAsync(string fileName, CancellationToken cancellationToken)
	{
		var value = await ReadValueAsync(fileName, cancellationToken);
		var model = value.GetValue<SettingsStorage>("Scheme")?.GetValue<SettingsStorage>("Model");
		var nodes = model?.GetValue<SettingsStorage[]>("Nodes") ?? [];
		var composites = new List<Guid>();

		foreach (var node in nodes)
		{
			// A lesson may use a cube the student builds in it. Such a node carries the identity of that
			// composition beside its own, which is how it is told apart from a built-in element.
			if (node.GetValue<SettingsStorage>("Settings")?.ContainsKey("PublishedId") == true)
				composites.Add(node.GetValue<Guid>("TypeId"));
		}

		return composites;
	}

	/// <summary>The instruments a schema names outright, rather than taking from the strategy.</summary>
	/// <param name="composition">Composition to look at.</param>
	/// <returns>Identifiers of the instruments its blocks hold.</returns>
	public static IEnumerable<string> SecurityIds(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		// A variable left empty stands for the strategy's own instrument; one holding an instrument names
		// what the replay has to be able to serve.
		return composition
			.Elements
			.OfType<VariableDiagramElement>()
			.Select(v => v.Value)
			.OfType<Security>()
			.Select(s => s.Id)
			.Where(id => !id.IsEmpty());
	}

	/// <summary>Whether a file describes something that can be replayed at all.</summary>
	/// <param name="composition">Composition to look at.</param>
	/// <returns>True when it both receives market data and acts on it.</returns>
	public static bool IsRunnable(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		var elements = composition.Elements.Where(e => e is not null).ToArray();

		// A lesson is often a fragment: the maths of position sizing with no candles behind it, or a chart
		// with nothing that trades. Only what has both a source and a way to act can be replayed.
		return elements.Any(e => e is SubscriptionDiagramElement)
			&& elements.Any(e => e is PositionModifyElement or OrderRegisterDiagramElement or OrderReplaceDiagramElement);
	}

	/// <summary>Reads the composition stored in a gallery file.</summary>
	/// <param name="fileName">Full path of the schema file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The composition the file describes.</returns>
	public static async Task<CompositionDiagramElement> LoadAsync(string fileName, CancellationToken cancellationToken)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		var value = await ReadValueAsync(fileName, cancellationToken);
		var registry = await GetRegistryAsync(cancellationToken);
		var composition = registry.CreateComposition();
		registry.Deserialize(composition, value, ICompositionRegistryExtensions.NotSupported);

		return composition;
	}

	private static async Task<SettingsStorage> ReadValueAsync(string fileName, CancellationToken cancellationToken)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		// The gallery keeps the file the Designer wrote, so it is read back with the serializer that
		// wrote it rather than by plain JSON mapping, which leaves nested settings as raw tokens.
		using var stream = File.OpenRead(fileName);

		var storage = await JsonSerializer<SettingsStorage>.CreateDefault().DeserializeAsync(stream, cancellationToken)
			?? throw new InvalidOperationException($"'{fileName}' holds no settings.");

		// The gallery holds both shapes the Designer writes: a saved file, where the schema sits under
		// Content.Value beside its identifier, and a bare exported composition, which is that value on
		// its own. Both open in the Designer, so both are read here.
		var value = storage.ContainsKey("Content")
			? storage.GetValue<SettingsStorage>("Content").GetValue<SettingsStorage>("Value")
				?? throw new InvalidOperationException($"'{fileName}' has no 'Content.Value'.")
			: storage;

		if (!value.ContainsKey("Scheme"))
			throw new InvalidOperationException($"'{fileName}' holds no schema.");

		return value;
	}
}

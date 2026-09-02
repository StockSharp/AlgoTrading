namespace StockSharp.Tests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Serialization;

using StockSharp.Algo;
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

	/// <summary>Reads the composition stored in a gallery file.</summary>
	/// <param name="fileName">Full path of the schema file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The composition the file describes.</returns>
	public static async Task<CompositionDiagramElement> LoadAsync(string fileName, CancellationToken cancellationToken)
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

		var registry = await GetRegistryAsync(cancellationToken);
		var composition = registry.CreateComposition();
		registry.Deserialize(composition, value, ICompositionRegistryExtensions.NotSupported);

		return composition;
	}
}

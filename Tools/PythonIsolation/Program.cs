namespace StockSharp.PythonIsolation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Logging;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Strategies;
using StockSharp.Configuration;

/// <summary>
/// Loads every Python example in a process of its own.
/// </summary>
/// <remarks>
/// The test run loads all examples into one IronPython runtime, so an assembly one example
/// references stays visible to every example after it. An example that imports a type without
/// referencing its assembly then passes or fails depending on the order the tests run in. Here
/// each example starts from a fresh runtime: it is compiled, its strategy is created, and every
/// import nested in a function is executed too, because those run only when the strategy reaches
/// them during a backtest.
/// </remarks>
static class Program
{
	private const int _success = 0;
	private const int _failure = 1;
	private const int _usageFailure = 2;

	// Child exit codes, one per stage an example can fail at.
	private const int _compileFailure = 10;
	private const int _createFailure = 11;
	private const int _nestedImportFailure = 12;

	private const string _nestedImportsMarker = "# --- nested imports, executed at module level by PythonIsolation ---";

	private static readonly Regex _nestedImport = new(
		@"^(?<indent>[ \t]+)(?<stmt>(?:from\s+[\w.]+\s+import\s+(?:\([^)]*\)|(?:\\\n|[^\n])*))|(?:import\s+(?:\\\n|[^\n])*))",
		RegexOptions.Multiline);

	public static async Task<int> Main(string[] args)
	{
		try
		{
			if (args.Length == 2 && args[0] == "--file")
				return await CheckFileAsync(args[1]);

			return await RunAllAsync(args);
		}
		catch (ArgumentException error)
		{
			Console.Error.WriteLine(error.Message);
			PrintUsage();
			return _usageFailure;
		}
	}

	private static void PrintUsage()
	{
		Console.Error.WriteLine("""
			Usage: PythonIsolation [--api <dir>] [--jobs <n>] [--timeout <seconds>] [--filter <text>] [--report <file.json>]
			       PythonIsolation --file <example.py>

			Without --file, starts one process per Python example under API and reports those that fail.
			--filter keeps the examples whose path contains the text.
			""");
	}

	private sealed record Options(string Api, int Jobs, TimeSpan Timeout, string Filter, string Report);

	private static Options ParseOptions(string[] args)
	{
		var api = Path.Combine(FindRepositoryRoot(), "API");
		var jobs = Environment.ProcessorCount;
		var timeout = TimeSpan.FromSeconds(120);
		string filter = null;
		string report = null;

		for (var i = 0; i < args.Length; i++)
		{
			string value()
				=> i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

			switch (args[i])
			{
				case "--api": api = Path.GetFullPath(value()); break;
				case "--jobs": jobs = int.Parse(value()); break;
				case "--timeout": timeout = TimeSpan.FromSeconds(int.Parse(value())); break;
				case "--filter": filter = value(); break;
				case "--report": report = Path.GetFullPath(value()); break;
				default: throw new ArgumentException($"Unknown option {args[i]}.");
			}
		}

		if (jobs < 1)
			throw new ArgumentException("--jobs must be at least 1.");

		if (!Directory.Exists(api))
			throw new ArgumentException($"API directory not found: {api}");

		return new(api, jobs, timeout, filter, report);
	}

	private static string FindRepositoryRoot()
	{
		for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
		{
			if (File.Exists(Path.Combine(dir.FullName, "AlgoTrading.slnx")))
				return dir.FullName;
		}

		return Directory.GetCurrentDirectory();
	}

	private sealed record Result(string Path, string Outcome, int ExitCode, double Seconds, string Output);

	private static async Task<int> RunAllAsync(string[] args)
	{
		var options = ParseOptions(args);

		var files = Directory.EnumerateFiles(options.Api, "*.py", SearchOption.AllDirectories)
			.Where(f => Path.GetFileName(Path.GetDirectoryName(f)) == "PY")
			.Select(f => Path.GetRelativePath(options.Api, f).Replace('\\', '/'))
			.Where(f => options.Filter == null || f.Contains(options.Filter, StringComparison.OrdinalIgnoreCase))
			.Order(StringComparer.Ordinal)
			.ToArray();

		if (files.Length == 0)
		{
			Console.Error.WriteLine("No Python examples matched.");
			return _usageFailure;
		}

		Console.WriteLine($"Loading {files.Length} Python examples, one process each, {options.Jobs} at a time.");

		var results = new ConcurrentBag<Result>();
		var done = 0;
		var watch = Stopwatch.StartNew();

		await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = options.Jobs }, async (file, _) =>
		{
			var result = await RunChildAsync(options, file);
			results.Add(result);

			var count = Interlocked.Increment(ref done);

			if (result.Outcome != "ok")
				Console.WriteLine($"  {result.Outcome,-15} {file}");

			if (count % 100 == 0 || count == files.Length)
				Console.WriteLine($"[{count}/{files.Length}] {watch.Elapsed:hh\\:mm\\:ss}");
		});

		var ordered = results.OrderBy(r => r.Path, StringComparer.Ordinal).ToArray();
		var failed = ordered.Where(r => r.Outcome != "ok").ToArray();

		if (options.Report != null)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(options.Report));
			await File.WriteAllTextAsync(options.Report, JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true }));
			Console.WriteLine($"Report: {options.Report}");
		}

		Console.WriteLine();
		Console.WriteLine($"{ordered.Length} examples, {ordered.Length - failed.Length} load on their own, {failed.Length} do not ({watch.Elapsed:hh\\:mm\\:ss}).");

		foreach (var group in failed.GroupBy(r => r.Outcome).OrderBy(g => g.Key))
			Console.WriteLine($"  {group.Key}: {group.Count()}");

		foreach (var result in failed)
		{
			Console.WriteLine();
			Console.WriteLine($"{result.Outcome}: {result.Path}");
			Console.WriteLine(Indent(result.Output.Trim()));
		}

		return failed.Length == 0 ? _success : _failure;
	}

	private static async Task<Result> RunChildAsync(Options options, string relativePath)
	{
		var self = Environment.ProcessPath;
		var start = new ProcessStartInfo
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};

		// Run through the apphost when there is one; under "dotnet <dll>" the host is dotnet itself.
		if (Path.GetFileNameWithoutExtension(self).EqualsIgnoreCase("dotnet"))
		{
			start.FileName = self;
			start.ArgumentList.Add(typeof(Program).Assembly.Location);
		}
		else
			start.FileName = self;

		start.ArgumentList.Add("--file");
		start.ArgumentList.Add(Path.Combine(options.Api, relativePath));

		var watch = Stopwatch.StartNew();
		using var process = Process.Start(start);
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();

		using var timeout = new CancellationTokenSource(options.Timeout);

		try
		{
			await process.WaitForExitAsync(timeout.Token);
		}
		catch (OperationCanceledException)
		{
			process.Kill(entireProcessTree: true);
			await process.WaitForExitAsync();
			return new(relativePath, "timeout", -1, watch.Elapsed.TotalSeconds, await stderr);
		}

		var output = (await stderr).Trim();

		if (output.Length == 0)
			output = (await stdout).Trim();
		else
			await stdout;

		var outcome = process.ExitCode switch
		{
			_success => "ok",
			_compileFailure => "compile",
			_createFailure => "create",
			_nestedImportFailure => "nested-import",
			_ => "crash",
		};

		return new(relativePath, outcome, process.ExitCode, Math.Round(watch.Elapsed.TotalSeconds, 2), outcome == "ok" ? "" : output);
	}

	private static async Task<int> CheckFileAsync(string path)
	{
		path = Path.GetFullPath(path);

		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"File not found: {path}");
			return _usageFailure;
		}

		// Each child extracts the Python helpers into a directory of its own, so that parallel
		// children never write the same files.
		var home = Path.Combine(Path.GetTempPath(), "PythonIsolation", Environment.ProcessId.To<string>());
		PathsHolder.CompanyPath = home;

		try
		{
			using var logManager = new LogManager();
			await CompilationExtensions.Init(Paths.FileSystem, logManager.Application, [], CancellationToken.None);

			var text = await File.ReadAllTextAsync(path);
			var name = Path.GetFileNameWithoutExtension(path);

			using (var code = new CodeInfo { Name = name, Text = text, Language = FileExts.Python })
			{
				if (!await TryCompileAsync(code))
					return _compileFailure;

				try
				{
					code.ObjectType.CreateInstance<Strategy>();
				}
				catch (Exception error)
				{
					Console.Error.WriteLine(error);
					return _createFailure;
				}
			}

			var nested = _nestedImport.Matches(text).Select(m => m.Groups["stmt"].Value.Trim()).Distinct().ToArray();

			if (nested.Length == 0)
				return _success;

			var probe = new StringBuilder(text.TrimEnd()).Append("\n\n").Append(_nestedImportsMarker).Append('\n');

			foreach (var statement in nested)
				probe.Append(statement).Append('\n');

			using var probeCode = new CodeInfo { Name = name, Text = probe.ToString(), Language = FileExts.Python };

			if (!await TryCompileAsync(probeCode))
			{
				Console.Error.WriteLine("Nested imports executed:");
				Console.Error.WriteLine(Indent(nested.JoinN()));
				return _nestedImportFailure;
			}

			return _success;
		}
		finally
		{
			try
			{
				Directory.Delete(home, recursive: true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	private static async Task<bool> TryCompileAsync(CodeInfo code)
	{
		IEnumerable<CompilationError> errors;

		try
		{
			errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, CancellationToken.None);
		}
		catch (Exception error)
		{
			Console.Error.WriteLine(error);
			return false;
		}

		var failures = errors.ErrorsOnly().ToArray();

		foreach (var failure in failures)
			Console.Error.WriteLine(failure);

		return failures.Length == 0;
	}

	private static string Indent(string text)
		=> string.Join('\n', text.Split('\n').Select(l => "    " + l));
}

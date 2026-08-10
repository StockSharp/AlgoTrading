![StockSharp logo](logo.png)

# StockSharp algorithmic trading examples

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This is the official StockSharp repository of algorithmic trading examples. It combines a large, organized catalog of API strategies with visual Strategy Designer samples, educational material, and automated checks that keep the examples buildable.

The repository is intended for learning, research, prototyping, and regression testing. The strategies illustrate trading ideas and StockSharp APIs; they are not ready-made investment recommendations.

## Start here

| Goal | Location |
|---|---|
| Browse strategies by trading idea | [API strategy catalog](API/README.md) |
| Study C# and Python implementations | [`API`](API/) |
| Explore visual schemas and Designer examples | [`Designer`](Designer/) |
| Compile and backtest one C# strategy | [`Backtester`](Backtester/) |
| Review the automated strategy test harness | [`Tests`](Tests/) |

## What the repository contains

### API strategy catalog

The [`API`](API/) directory contains strategy examples ranging from familiar building blocks—moving-average crosses, breakouts, momentum, volatility, and mean reversion—to pairs trading, arbitrage, market making, portfolio methods, order-flow models, machine-learning experiments, and many specialized variations.

The catalog groups strategies by their primary trading idea, while the filesystem uses numbered range directories so GitHub can display the collection efficiently. A typical example looks like this:

```text
API/0001-0100/0001_MA_CrossOver/
├── CS/
│   ├── MaCrossoverStrategy.cs
│   └── logo.svg
├── PY/
│   ├── ma_crossover_strategy.py
│   └── logo.svg
├── README.md
├── README_ru.md
├── README_zh.md
├── README_es.md
├── README_de.md
├── README_pt.md
└── README_ja.md
```

Every API example provides the same strategy idea in both C# and Python. Its documentation explains the concept, parameters, signal logic, and risk considerations in seven languages. The transparent SVG logos identify both the strategy and its implementation language and remain readable in light and dark themes.

### Strategy Designer examples

The [`Designer`](Designer/) directory contains visual schemas, reusable strategy types, and educational examples for the [StockSharp Strategy Designer](https://doc.stocksharp.com/en/topics/designer.html). These samples are useful when you prefer to assemble and inspect a strategy graphically instead of starting with source code.

### Build and test utilities

The repository includes two small .NET projects:

- [`Backtester`](Backtester/) dynamically compiles a selected C# strategy and runs it against bundled sample history data.
- [`Tests`](Tests/) compiles the API examples and exercises them through StockSharp's historical emulation environment.

The test project uses a source generator, so ordinary strategies do not need handwritten test methods. Each generated test runs a strategy on sample market data, verifies that it produces orders and trades, and checks cloning and settings serialization. Strategies requiring multiple instruments or custom setup have explicit overrides in the test project.

Before the .NET build, [`Tools/validate_api_structure.py`](Tools/validate_api_structure.py) performs the fast structural checks: numbered directory placement, C#/Python parity, required translations, source-file presence, and stale language-availability claims.

## Prerequisites

To build the complete solution locally, install:

- the .NET 10 SDK;
- Python 3 for the structure validator;
- a sibling checkout of the StockSharp platform repository.

The project references expect the following directory layout:

```text
<workspace>/
├── AlgoTrading/
└── StockSharp (GitHub)/
```

Clone this repository as `AlgoTrading` and the [StockSharp platform repository](https://github.com/StockSharp/StockSharp) as `StockSharp (GitHub)` under the same parent directory.

## Validate, build, and test

Run the fast repository checks first:

```bash
python Tools/validate_api_structure.py
```

Then build and test the solution in the same configuration used by CI:

```bash
dotnet build AlgoTrading.slnx --configuration Release
dotnet test AlgoTrading.slnx --no-build --configuration Release
```

To run only one generated strategy test, filter by the PascalCase strategy folder name. For example:

```bash
dotnet test Tests/Tests.csproj --no-build --configuration Release \
  --filter "FullyQualifiedName~MaCrossover"
```

To compile and backtest one C# example directly:

```bash
dotnet run --project Backtester/Backtester.csproj -- \
  API/0001-0100/0001_MA_CrossOver/CS/MaCrossoverStrategy.cs
```

## Using the examples

Choose a strategy from the [catalog](API/README.md), read its assumptions and parameters, and compare the C# and Python implementations. Treat each example as a starting point: select suitable market data, commissions, slippage, latency, position sizing, and risk limits before evaluating the idea.

For visual development, install [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/), open its [Strategy Gallery](https://doc.stocksharp.com/en/topics/designer/strategy_gallery.html), and use the schemas in [`Designer`](Designer/) as learning material or prototypes.

Always validate a modified strategy on out-of-sample data and in simulation before considering live execution. A backtest demonstrates behavior on a particular dataset; it does not establish future profitability.

## Contributing

Contributions that improve correctness, clarity, coverage, or educational value are welcome. When adding or changing an API strategy:

1. Keep the strategy in its numbered range directory.
2. Maintain both C# and Python implementations.
3. Keep the seven localized README files aligned with the actual parameters and behavior.
4. Add or update the language-specific transparent SVG logos when the strategy identity changes.
5. Run the structure validator, Release build, and relevant tests before opening a pull request.

Ordinary strategies are discovered automatically by the test source generator. Add a handwritten override only when the example needs custom securities, portfolios, market data, or other setup that the standard harness cannot provide.

## Resources

- [StockSharp website](https://stocksharp.com/)
- [Documentation](https://doc.stocksharp.com/en/)
- [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)
- [Community chat](https://stocksharp.com/en/chat/)
- [Issue tracker](https://github.com/StockSharp/AlgoTrading/issues)

## License and risk notice

See [LICENSE](LICENSE) and [NOTICE](NOTICE) for the terms that apply to this repository.

Algorithmic trading involves substantial risk. Examples in this repository are provided for educational and technical purposes without any guarantee of performance. You are responsible for reviewing the code, validating assumptions, and applying appropriate operational and financial risk controls before any real-money use.

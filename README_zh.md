![StockSharp 标志](logo.png)

# StockSharp 算法交易示例

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

这是 StockSharp 官方算法交易示例仓库。仓库汇集了结构清晰的大型 API 策略目录、Strategy Designer 可视化示例、教学材料，以及用于确保示例可编译和可运行的自动化检查。

本仓库适用于学习、研究、原型开发和回归测试。这里的策略用于展示交易思想和 StockSharp API 的使用方式，并非可直接采用的投资建议。

## 从这里开始

| 目标 | 位置 |
|---|---|
| 按交易思想浏览策略 | [API 策略目录](API/README_zh.md) |
| 学习 C# 和 Python 实现 | [`API`](API/) |
| 查看可视化结构和 Designer 示例 | [`Designer`](Designer/) |
| 编译并回测单个 C# 策略 | [`Backtester`](Backtester/) |
| 查看自动化策略测试框架 | [`Tests`](Tests/) |

## 仓库内容

### API 策略目录

[`API`](API/) 目录既包含移动平均线交叉、突破、动量、波动率和均值回归等常见方法，也包含配对交易、套利、做市、投资组合方法、订单流模型、机器学习实验以及许多专门化变体。

目录页面按主要交易思想对策略进行分类；文件系统则使用编号区间组织策略，以便 GitHub 高效显示这个大型集合。典型示例结构如下：

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

每个 API 示例都用 C# 和 Python 实现同一个策略思想。七种语言的文档说明策略原理、参数、信号逻辑和风险注意事项。透明 SVG 标志同时体现策略含义和实现语言，并可在浅色与深色主题下清晰显示。

### Strategy Designer 示例

[`Designer`](Designer/) 目录包含 [StockSharp Strategy Designer](https://doc.stocksharp.com/en/topics/designer.html) 的可视化结构、可复用策略类型和教学示例。如果你希望以图形方式组装和分析策略，而不是直接从源代码开始，这些示例会很有帮助。

### 构建与测试工具

仓库包含两个小型 .NET 项目：

- [`Backtester`](Backtester/) 动态编译指定的 C# 策略，并使用随附的示例历史数据进行回测。
- [`Tests`](Tests/) 编译 API 示例，并在 StockSharp 历史仿真环境中运行验证。

测试项目使用源代码生成器，因此普通策略无需手工添加测试方法。每个生成的测试都会在示例市场数据上运行策略，验证其是否产生订单和成交，并检查克隆与设置序列化。需要多个证券或特殊初始化的策略在测试项目中提供显式覆盖实现。

在 .NET 构建之前，[`Tools/validate_api_structure.py`](Tools/validate_api_structure.py) 会执行快速结构检查：编号目录位置、C#/Python 完整配对、必需翻译、源文件是否存在，以及是否包含过时的“某语言版本不可用”说明。

## 环境要求

要在本地完整构建解决方案，请安装：

- .NET 10 SDK；
- 用于结构验证器的 Python 3；
- 与本仓库同级放置的 StockSharp 平台仓库。

项目引用要求以下目录结构：

```text
<workspace>/
├── AlgoTrading/
└── StockSharp (GitHub)/
```

请将本仓库克隆为 `AlgoTrading`，并将 [StockSharp 平台仓库](https://github.com/StockSharp/StockSharp) 克隆为同一父目录下的 `StockSharp (GitHub)`。

## 验证、构建和测试

首先运行快速仓库检查：

```bash
python Tools/validate_api_structure.py
```

然后使用与 CI 相同的配置构建并测试解决方案：

```bash
dotnet build AlgoTrading.slnx --configuration Release
dotnet test AlgoTrading.slnx --no-build --configuration Release
```

若只运行一个生成的策略测试，请使用策略文件夹名称对应的 PascalCase 名称进行筛选。例如：

```bash
dotnet test Tests/Tests.csproj --no-build --configuration Release \
  --filter "FullyQualifiedName~MaCrossover"
```

直接编译并回测一个 C# 示例：

```bash
dotnet run --project Backtester/Backtester.csproj -- \
  API/0001-0100/0001_MA_CrossOver/CS/MaCrossoverStrategy.cs
```

## 使用示例

从[策略目录](API/README_zh.md)中选择一个策略，阅读其前提和参数，并对比 C# 与 Python 实现。请把每个示例视为起点：在评估策略思想前，应设置合适的市场数据、手续费、滑点、延迟、仓位管理和风险限制。

进行可视化开发时，可安装 [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)，打开其中的 [Strategy Gallery](https://doc.stocksharp.com/en/topics/designer/strategy_gallery.html)，并将 [`Designer`](Designer/) 目录中的结构作为学习材料或原型。

在考虑实盘执行之前，应使用样本外数据和仿真环境验证修改后的策略。回测只能说明策略在特定数据集上的行为，不能证明未来盈利能力。

## 参与贡献

欢迎任何能够提高正确性、清晰度、覆盖范围或教学价值的贡献。添加或修改 API 策略时，请遵循以下规则：

1. 将策略放入正确的编号区间目录。
2. 同时维护 C# 和 Python 实现。
3. 保持七个本地化 README 与真实参数和行为一致。
4. 当策略形象发生变化时，更新对应实现语言的透明 SVG 标志。
5. 提交 pull request 前运行结构验证器、Release 构建和相关测试。

普通策略会由测试源代码生成器自动发现。只有在示例需要特殊证券、投资组合、市场数据或标准测试框架无法提供的其他初始化时，才应添加手工覆盖测试。

## 相关资源

- [StockSharp 网站](https://stocksharp.com/)
- [文档](https://doc.stocksharp.com/en/)
- [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)
- [社区聊天](https://stocksharp.com/en/chat/)
- [问题跟踪器](https://github.com/StockSharp/AlgoTrading/issues)

## 许可证与风险提示

本仓库适用的条款请参见 [LICENSE](LICENSE) 和 [NOTICE](NOTICE)。

算法交易具有重大风险。本仓库示例仅用于教育和技术目的，不对任何交易表现作出保证。在投入真实资金之前，你有责任审查代码、验证假设，并采取适当的运营与财务风险控制措施。

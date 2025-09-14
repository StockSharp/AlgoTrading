# DDE 示例策略
[English](README.md) | [Русский](README_ru.md)

该示例策略演示如何通过 DDE（动态数据交换）接口将 StockSharp 的数据发送到 Windows 应用程序。策略基于选定的K线序列计算指数移动平均线（EMA），并通过 DDE 发布最新值。

## 详情

- **用途**：使用传统 DDE 机制向外部软件导出指标值。
- **指标**：EMA。
- **信号**：无，本策略不提交订单。
- **DDE 项目**：
  - `COMPANY!Value` – 固定占位文本。
  - `TIME!Value` – 最近完成K线的时间。
  - `A!B` – 格式化的 EMA 值。

## 参数

- `EmaLength` – EMA 周期（默认 21）。
- `CandleType` – 用于计算的K线类型（默认 1 分钟）。

策略需要存在名为 `MT4.DDE.2` 的窗口来完成 DDE 通信。每当一根K线完成时，上述项目都会更新。

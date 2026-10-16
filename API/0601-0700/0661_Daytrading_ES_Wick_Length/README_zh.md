# 日内交易ES影线长度策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

当K线的上下影线总长度超过其加上偏移量的移动平均值时，该策略建立多头仓位，并在持有固定数量的K线后退出。

### 可检验的交易条件（C# 与 Python）

对每根**已完成的1分钟K线**，总影线长度 `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)`。采用当前K线**之前的20根已完成K线**计算 `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)`，若全部成交量为零则回退到简单平均；`MaType=Simple` 直接使用算术平均。`MaOffset=10` 的单位是**证券最小价格跳动**，入场阈值为 `VWMA(W) + 10 × Security.PriceStep`。预热20根后，仅在空仓且 `W > 阈值` 时**开多仓**。经过**之后18根已完成K线**，卖出全部剩余持仓。不做空，不设置止损或止盈。默认 `MaLength=20`、`MaType=VolumeWeighted`、`MaOffset=10`、`HoldPeriods=18`、`CandleType=1分钟`。



## 细节

- **入场条件**：影线总长度大于带有偏移的移动平均。
- **出场条件**：持有 `Hold periods` 根K线后平仓。
- **多空方向**：仅做多。
- **止损**：无。
- **默认值**：
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1 分钟K线
- **过滤器**：
  - 类别：波动性
  - 方向：多头
  - 指标：移动平均、影线长度
  - 止损：无
  - 复杂度：简单
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等

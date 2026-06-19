# Golden Transform 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 Rate of Change 指标、三重 Hull-TRIX、Hull MA 过滤器以及平滑的 Fisher Transform。当 ROC 上穿 TRIX 且 TRIX 低于零并且开盘价高于 Hull MA 时开多仓；相反条件下开空仓。持仓在出现相反交叉或平滑后的 Fisher 超过阈值并反转时平仓。

## 细节

- **入场条件**：
  - **做多**：`ROC 上穿 TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **做空**：`ROC 下穿 TRIX` && `TRIX > 0` && `Open < Hull MA`
- **多空方向**：多空双向
- **出场条件**：
  - 多头：`ROC 下穿 TRIX` 或 (`Fisher HMA > 1.5` && `Fisher HMA 下穿前一 Fisher`)
  - 空头：`ROC 上穿 TRIX` 或 (`Fisher HMA < -1.5` && `Fisher HMA 上穿前一 Fisher`)
- **止损**：无
- **默认值**：
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：ROC、Hull MA、Fisher Transform
  - 止损：无
  - 复杂度：中
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中

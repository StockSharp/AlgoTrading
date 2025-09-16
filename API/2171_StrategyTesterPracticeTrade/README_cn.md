# Strategy Tester Practice Trade 策略
[English](README.md) | [Русский](README_ru.md)

用于在策略测试器中练习手动交易的工具。策略监控指定目录中的命令文件，当文件出现时执行市场订单。在命令目录中创建空的 `buy.txt`、`sell.txt` 或 `close.txt`，在下一根完成的K线触发买入、卖出或平仓。

## 细节

- **入场条件**: 命令目录中存在 `buy.txt` 或 `sell.txt`
- **多空方向**: 双向
- **出场条件**: `close.txt` 关闭所有持仓
- **止损**: 无
- **默认值**:
  - `LotSize` = 1
  - `CommandDir` = 系统临时目录
  - `CandleType` = 1分钟K线
- **过滤器**:
  - 分类: 工具
  - 方向: 双向
  - 指标: 无
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 低

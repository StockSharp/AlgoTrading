# Bill Williams Trader 策略

该策略基于 **Alligator** 指标和 **Fractals**，是 Bill Williams 交易方法的简化实现。

## 工作原理

- 计算 Alligator 线（SMMA）：
  - **Jaw** 长度 13
  - **Teeth** 长度 8
  - **Lips** 长度 5
- 在完成的蜡烛上检测向上和向下的分形。
- 当价格突破位于 Teeth 之上的最后一个上分形时 **买入**。
- 当价格跌破位于 Teeth 之下的最后一个下分形时 **卖出**。
- 当收盘价跌破 Lips 线时 **平仓多单**。
- 当收盘价突破 Lips 线上方时 **平仓空单**。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `JawLength` | Alligator 下颚 SMMA 周期 | 13 |
| `TeethLength` | Alligator 牙齿 SMMA 周期 | 8 |
| `LipsLength` | Alligator 嘴唇 SMMA 周期 | 5 |
| `CandleType` | 计算所用的蜡烛类型 | 15 分钟蜡烛 |

所有参数都支持在策略参数界面中优化。

## 使用方法

1. 编译解决方案：
   ```bash
   dotnet build
   ```
2. 在 StockSharp 环境中运行该策略，并选择所需的证券和时间框架。

## 备注

该示例演示了高层 API 的使用，并未实现复杂的头寸管理或风险控制。

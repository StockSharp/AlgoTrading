# TSI WPR 交叉策略

该策略基于由 Williams %R 振荡器计算的真实强度指数 (TSI) 与其信号线的交叉进行交易。
当 TSI 上穿平滑的信号线时做多，当 TSI 下穿信号线时做空。

## 参数
- **Candle Type**：用于计算的蜡烛时间框。
- **Williams %R Period**：Williams %R 指标的周期。
- **Short Length**：TSI 计算中的短期 EMA 长度。
- **Long Length**：TSI 计算中的长期 EMA 长度。
- **Signal Length**：对 TSI 进行平滑形成信号线的 EMA 长度。

## 交易规则
1. 计算每根完成蜡烛的 Williams %R 值。
2. 将该值输入真实强度指数指标。
3. 用 EMA 平滑 TSI 获得信号线。
4. 当 TSI 上穿信号线时买入。
5. 当 TSI 下穿信号线时卖出。
6. 出现新信号时关闭相反方向的仓位。

## 备注
- 策略使用高级 API 自动订阅蜡烛数据。
- StartProtection 在启动时启用，用于基础风险管理。
- 创建图表区域以可视化 TSI、信号线和执行的交易。

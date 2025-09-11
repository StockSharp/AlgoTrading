# Vortex Cross with MA Confirmation Strategy

本策略利用 Vortex 指标识别趋势反转，并使用平滑移动平均线确认信号。当正向 Vortex 上穿负向且价格位于平滑均线上方时开多；当负向 Vortex 上穿正向且价格低于该线时做空。

## 参数
- **Vortex Length** – Vortex 指标周期。
- **SMA Length** – 基础 SMA 周期。
- **Smoothing Length** – 平滑均线周期。
- **MA Type** – 平滑方法。
- **Candle Type** – 使用的蜡烛类型。

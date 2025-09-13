# Exp Hull Trend 策略

## 概述

Exp Hull Trend 策略基于 Hull 移动平均线 (HMA)。算法比较中间的 Hull 计算值与其平滑后的版本。当快速 Hull 线从下向上穿过平滑线时，策略开多单；当快速线从上向下穿过平滑线时，策略开空单。

## 策略逻辑

1. 计算收盘价的加权移动平均线 (WMA)，周期为 **Length / 2**。
2. 计算收盘价的第二个 WMA，周期为 **Length**。
3. 构造中间 Hull 值：`fast = 2 * WMA(Length/2) - WMA(Length)`。
4. 使用周期 `sqrt(Length)` 的 WMA 对该值进行平滑，得到最终的 Hull 值 `slow`。
5. 信号生成：
   - **做多** – 当 `fast` 上穿 `slow`。
   - **做空** – 当 `fast` 下穿 `slow`。
6. 出现反向信号时仓位反转。保护性订单通过 `StartProtection` 处理。

## 参数

| 名称 | 描述 |
|------|------|
| `Hull Length` | Hull 计算的基础周期，决定 WMA 的灵敏度。|
| `Candle Type` | 用于指标计算的 K 线时间框架。|

## 备注

- 策略仅在收盘完成的 K 线上工作。
- 指标值通过高级 API 绑定，无需手动维护数据集合。
- 交易量来自策略设置；当方向改变时仓位将被反转。

# Ichimokuと200 SMAフィルターを使用した精錬SMA EMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期SMA/EMAクロスオーバーとIchimoku Cloudおよび200期間SMAフィルターを組み合わせます。SMAがEMAを上抜け、価格が雲と200 SMAの上にある場合にロングに入ります。SMAがEMAを下抜け、価格が雲と200 SMAの下にある場合に売ります。

## 詳細

- **エントリー条件:**
  - **ロング:** SMAがEMAを上抜け、価格がIchimoku Cloudの上、価格が200 SMAの上。
  - **ショート:** SMAがEMAを下抜け、価格がIchimoku Cloudの下、価格が200 SMAの下。
- **エグジット条件:** 逆シグナル。
- **インジケーター:** Ichimoku Cloud、SMA、EMA、200 SMA。

# Parabolic 売買トレンドフォロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SAR と移動平均クロスオーバーを組み合わせます。
価格が長期トレンド SMA を上回り、短期 EMA が長期 EMA を上抜け、SAR が強気のときにロングエントリーします。
ショートエントリーは逆の条件を使用します。
ストップロスはトレンド SMA に設定し、テイクプロフィットはリスク/リワード比を使用します。

## 詳細

- **エントリー**:
  - **ロング**: 価格 > トレンド SMA、短期 EMA が長期 EMA を上抜け、SAR 強気
  - **ショート**: 価格 < トレンド SMA、短期 EMA が長期 EMA を下抜け、SAR 弱気
- **エグジット**:
  - トレンド SMA でストップ
  - テイクプロフィット = リスク/リワード × エントリーからトレンド SMA までの距離
- **インジケーター**: Parabolic SAR, SMA, EMA
- **時間軸**: 設定可能
- **タイプ**: トレンドフォロー

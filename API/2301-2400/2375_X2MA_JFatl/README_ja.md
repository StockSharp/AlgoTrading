# X2MA JFatlクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderのエキスパート`Exp_X2MA_JFatl`をStockSharp向けに適応したものです。高速の単純移動平均（SMA）と低速のJurik移動平均（JMA）、そしてトレンド方向を確認するための追加のJurikフィルターを組み合わせています。高速平均が低速平均をクロスし、かつ価格がフィルターと同じ側にある場合に取引がオープンされます。価格がフィルターに逆らって動くか、逆のクロスオーバーが発生するとポジションはクローズされます。

## 詳細

- **エントリー条件**:
  - **ロング**: `SMA_fast`が`JMA_slow`を上抜けし、`Close` > `JMA_filter`。
  - **ショート**: `SMA_fast`が`JMA_slow`を下抜けし、`Close` < `JMA_filter`。
- **エグジット条件**:
  - 価格がフィルターの反対側に移動する。
  - 平均線の逆クロスオーバー。
- **ロング/ショート**: 両方。
- **ストップ**: デフォルトでは使用しない。
- **デフォルト値**:
  - `Fast MA Length` = 5.
  - `Slow MA Length` = 12.
  - `Filter Length` = 20.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数（SMA、JMA）
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

# StockSharp Strategy Designer における 3 Black Crows トレンド戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

[Strategy Designer](https://doc.stocksharp.com/topics/designer.html) の「3 Black Crows Trend」戦略は、特定の弱気反転ローソク足パターンを使用して、株式市場における潜在的な下降動向を予測します。この自動化された取引スキーマは、重要な価格パターンを認識し対応するために綿密に設計されており、弱気トレンドから利益を得ることを目指しています。

![schema](schema.png)

## 戦略の詳細

### パターン検出：3 Black Crows

- **説明**：このモジュールは「3 Black Crows」[パターン](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)を識別します。このパターンは上昇トレンド後の潜在的な弱気反転を示します。パターンは、それぞれの始値より低い終値で引ける3本連続の長実体ローソク足で構成され、各セッションの始値は前のローソク足の実体内に位置します。
- **条件**：
  - ローソク足 1：Open > Close
  - ローソク足 2：Open > Close かつ Open < Previous Open
  - ローソク足 3：Open > Close かつ Open < Previous Open

### 取引実行

- **注文タイプ**：成行[注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **エントリー**：「3 Black Crows」パターンの確認時に売り注文を発動します。
- **エグジット戦略**：
  - **テイクプロフィット**：エントリー価格より3%上に設定。
  - **ストップロス**：エントリー価格より1%下に設定。
- **リスク管理**：戦略はトレーリングなしで初期の[ストップロスとテイクプロフィット](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)設定を厳守します。

### 取引条件

- **頻度**：[日次タイムフレーム](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)で動作し、各取引日の終わりに新しいローソク足の形成を処理します。
- **成行注文**：現在の市場価格で[取引を行う](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ことにより、迅速な実行を確保します。

## 実装の詳細

- **プラットフォーム**：パターン検出と自動取引実行のための包括的な機能を提供する StockSharp プラットフォームで実装されています。
- **設定**：
  - **ログレベル**：詳細な操作上の洞察を得るために設定可能。
  - **パラメーター表示**：操作の透明性のためのカスタマイズ可能な表示設定。
  - **Null 値の処理**：堅牢性と信頼性を高めるための Null 値の設定可能な処理。

## 結論

「3 Black Crows Trend」戦略は、弱気反転パターンの識別と活用に注力するトレーダー向けに設計されています。精確なパターン認識と厳格な取引実行ルールを組み合わせ、弱気市場シナリオにおける潜在的な収益性を高めることを目指しています。

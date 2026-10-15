# Swing Cyborg戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Swing Cyborgは、トレーダー自身のトレンド予測に基づいて執行を自動化する裁量支援ツールです。ユーザーは期待するトレンド方向とその有効な時間ウィンドウを定義します。戦略はRSIインジケーターでエントリーを確認し、固定ターゲットでエグジットを管理します。

## パラメーター
- `Volume` – 注文量（ロット単位）。
- `TrendPrediction` – 期待するトレンド方向（Uptrend または Downtrend）。
- `TrendTimeframe` – RSIと取引に使用する時間軸（M30、H1またはH4）。
- `TrendStart` – ユーザー定義のトレンド期間の開始。
- `TrendEnd` – ユーザー定義のトレンド期間の終了。
- `Aggressiveness` – マネー管理プリセット：
  - 低: テイクプロフィット300pips、ストップロス200pips。
  - 中: テイクプロフィット500pips、ストップロス250pips。
  - 高: テイクプロフィット600pips、ストップロス300pips。

## トレードロジック
1. 選択した時間軸で新しいローソク足を待つ。
2. 現在時刻が`TrendStart`と`TrendEnd`の間にある場合のみ取引する。
3. RSI(14)を計算する。
4. オープンポジションがない場合：
   - `TrendPrediction`がUptrendでRSI ≤ 65 → 買い。
   - `TrendPrediction`がDowntrendでRSI ≥ 35 → 売り。
5. `StartProtection`は利益または損失が事前定義されたレベルに達すると自動的にポジションを閉じる。

この戦略は確定したローソク足で動作し、既存のポジションがアクティブな間は新しいポジションを開かない。

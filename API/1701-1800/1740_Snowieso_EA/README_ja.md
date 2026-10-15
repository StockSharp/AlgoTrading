# Snowieso戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、トレンド方向を確認するために、高速・低速の**線形加重移動平均 (LWMA)**と**MACD**、**カウフマン適応型移動平均 (KAMA)** を組み合わせています。

## 仕組み
1. 選択した時間軸のローソク足を購読する。
2. Fast LWMA、Slow LWMA、MACDおよびKAMAの値を計算する。
3. **ロングエントリー**: 高速LWMAが低速LWMAを上抜け、MACDヒストグラムがプラス、KAMAが上昇しているときに発生。
4. **ショートエントリー**: 高速LWMAが低速LWMAを下抜け、MACDヒストグラムがマイナス、KAMAが下落しているときに発生。
5. `StartProtection`を使用して固定のストップロスとテイクプロフィットを適用。

この戦略は新規ポジションを開く前に反対方向のポジションを決済し、インジケーターと取引をチャート上に視覚化します。

## パラメーター
- `FastLength` – 高速LWMAの期間。
- `SlowLength` – 低速LWMAの期間。
- `MacdFast`, `MacdSlow`, `MacdSignal` – MACD設定。
- `KamaLength` – KAMAのルックバック期間。
- `StopLossPoints` – 価格ポイントでの絶対ストップロス。
- `TakeProfitPoints` – 価格ポイントでの絶対テイクプロフィット。
- `CandleType` – 処理するローソク足の時間軸。

## 使用方法
選択した銘柄に戦略をデプロイしてください。アルゴリズムは自動的にローソク足を購読し、インジケーターシグナルに基づいてポジションを管理します。データバインディングと注文執行にはハイレベルAPIが使用されます。

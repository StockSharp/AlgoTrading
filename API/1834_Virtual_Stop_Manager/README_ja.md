# 仮想ストップ・マネージャー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderアドバイザー「VR---STEALS-3-EN」から変換された戦略。隠れた注文管理機能を実装します：ストップロス、テイクプロフィット、トレーリングストップ、ブレークイーブン。戦略は最初のローソク足でロングポジションを開き、取引所に目に見える保護注文を出すことなく仮想的に出口レベルを管理します。

## パラメーター
- **Volume**: 注文量。
- **Take Profit (points)**: 利益でポジションを閉じるための点数距離。
- **Stop Loss (points)**: 損失でポジションを閉じるための点数距離。
- **Trailing Stop (points)**: 最高値からのトレーリングストップの距離。
- **Breakeven (points)**: ストップロスをエントリー価格に移動させる利益点数。
- **Candle Type**: 処理に使用するローソク足シリーズ。

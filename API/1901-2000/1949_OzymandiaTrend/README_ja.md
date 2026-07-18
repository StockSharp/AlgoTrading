# OzymandiaTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ozymandiasインジケーターベースのトレンド戦略です。インジケーターはATRと高値・安値の移動平均を組み合わせて動的チャンネルを構築します。方向が弱気から強気に切り替わると、戦略は買いを入れてショートポジションをクローズします。弱気への切り替えでは売りを入れてロングポジションをクローズします。オプションのテイクプロフィットとストップロスのパラメーターでリスクを管理します。

## 詳細

- **エントリー条件**: Ozymandiasインジケーターの方向転換。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたは設定されたストップ。
- **ストップ**: テイクプロフィットとストップロス。
- **デフォルト値**:
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Ozymandias (ATR + MA)
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 4h
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

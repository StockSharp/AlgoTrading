# BB HeikinAshi エントリー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashiローソク足を使用したBollinger Bands戦略。

システムはBollinger Bandsの下限バンドに触れた2〜3本の連続した弱気Heikin Ashiバーを待ちます。バンドを上回って終値をつけた強気ローソク足がロングエントリーを引き起こします。ショートは逆方向に機能します。ポジションの半分は最初の目標でクローズし、残りはトレーリングストップで保護されます。

## 詳細

- **エントリー条件**: Bollinger Bandsの付近での連続Heikin Ashiローソク足のリバーサル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 部分的な利益確定とトレーリングストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Heikin Ashi, Bollinger Bands
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

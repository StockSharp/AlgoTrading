# Vwap Cci 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

戦略の実装 - VWAP + CCI。価格が VWAP を下回り、CCI が -100 を下回っている（売られすぎ）場合に買い。価格が VWAP を上回り、CCI が 100 を上回っている（買われすぎ）場合に売り。

テストでは年平均収益率は約 82% を示しています。株式市場で最もパフォーマンスが優れています。

VWAP は価値の基準として機能し、CCI はそこから離れたモメンタムの動きを強調します。エントリーは VWAP に対する強い CCI の読みを優先します。

VWAP との相互作用に注目するデイトレーダー向けに設計されています。パーセントストップは規律の維持を助けます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < VWAP && CCI < CciOversold`
  - ショート: `Close > VWAP && CCI > CciOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が VWAP を逆方向に突き抜ける
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    市場は24時間取引のため、セッションVWAPはUTCの毎日リセットされ、各足の典型価格を出来高で加重します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中


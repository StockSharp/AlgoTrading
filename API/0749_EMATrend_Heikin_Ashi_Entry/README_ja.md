# EMAトレンド・Heikin Ashiエントリー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

上位時間軸のEMAトレンドフィルターと組み合わせて、Heikin Ashiローソク足にボリンジャーバンドを使用する戦略。上位時間軸の短期EMAが長期EMAを上回っている状態で、下バンドに触れる弱気なHeikin Ashiローソク足が連続した後にバンドの上で強気なローソク足が形成されると買い。逆の条件で売り。

エントリー後はリスクと同等の最初の目標を達成し、前のローソク足の極値を使用してトレーリングストップを適用します。

## 詳細

- **エントリー条件**:
  - ロング: 少なくとも2本の弱気HA足が下バンドに触れ、その後上バンドより上で強気の足が確認でき、上位時間軸の短期EMAが長期EMAを上回る
  - ショート: 少なくとも2本の強気HA足が上バンドに触れ、その後下バンドより下で弱気の足が確認でき、上位時間軸の短期EMAが長期EMAを下回る
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 最初の目標1R、その後前の安値でトレーリングストップ
  - ショート: 最初の目標1R、その後前の高値でトレーリングストップ
- **ストップ**: 前のローソク足の安値/高値
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: Bollinger Bands, Heikin Ashi, EMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

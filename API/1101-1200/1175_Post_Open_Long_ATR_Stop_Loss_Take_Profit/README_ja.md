# 寄り付き後ロング ATRストップロス・テイクプロフィット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、市場の寄り付き時に抵抗線のブレイクアウト後、価格がボリンジャーバンドの中心線付近に留まっている場合にロングポジションを建てます。EMA、RSI、ADX、ATRフィルターを使用し、ATRベースのストップロスとテイクプロフィットで退出します。

## 詳細

- **エントリー条件**:
  - **ロング**: 市場の寄り付き時に直近の抵抗線を上方ブレイク、価格がボリンジャーバンドの中心線付近、RSIが閾値を上回る、ADXが閾値を上回る、短期トレンドが上向きで押し目なし。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - ATRベースのストップロスまたはテイクプロフィット到達。
- **ストップ**:
  - ATRベースのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: Long
  - インジケーター: Bollinger Bands, EMA, RSI, ADX, ATR
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## 計算と執行

全インジケーターが形成済みの確定足だけを使用する。抵抗は現在足を除く直前 `ResistancePeriod` 本の高値の最大値で、エントリーは `Close > resistance` を必要とする。開始窓は足の開始時刻のUTC時刻を用い、開始を含み終了を含まず、日付をまたぐ窓にも対応する。`SessionStart` は対象銘柄の開始時刻に設定する。

Bollinger中心は現在足を含む終値SMA、偏差は母標準偏差。近接条件は `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`。短期EMAが長期EMAを上回り、Closeが短期EMAを上回ることが必要。押し戻しなしはシグナル足の `Close >= Open` と定義する。RSIとADXは閾値を厳密に上回る必要がある。

保有なし、未完了注文なしの場合だけ `Volume` を買う。エントリー足のWilder ATRでSL/TP距離を固定する。標準のローカル保護は実際の約定価格の下/上に水準を設定し（部分約定では出来高加重価格）、Level1最良買い気配と足の価格を監視して残りのロングを全決済する。開始窓の外でも決済は有効。後のATR変化で水準は動かない。ショート、EMA交差による決済、待機時間はない。

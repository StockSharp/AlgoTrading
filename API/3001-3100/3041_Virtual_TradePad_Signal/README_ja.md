# Virtual TradePad シグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader の VirtualTradePad ツールのマルチインジケーター・ダッシュボードロジックを再現します。トレンド、モメンタム、チャネルベースの12のシグナルを追跡し、設定可能な数のインジケーターが一致したときのみトレードします。目的は、元のパネルのビジュアル・センチメント・マトリクスを模倣し、完全自動化された StockSharp 戦略に変換することです。

## 仕組み

- **データ**: 選択されたローソク足タイプ（デフォルト: 15分）で単一の銘柄を取引します。
- **インジケーター**:
  - クロスオーバー方向のための高速/低速単純移動平均。
  - MACDラインとシグナルのクロスオーバー。
  - ストキャスティクス %K の過売り/過買いエグジット（20/80レベル）。
  - RSI 30/70 しきい値でのリバーサル。
  - CCI -100/+100 でのリバーサル。
  - Williams %R -80/-20 でのリバーサル。
  - ボリンジャーバンドのチャネル内への回帰ブレイクアウト。
  - 移動平均エンベロープのチャネル内への回帰ブレイクアウト。
  - Bill Williams Alligator の顎/歯/唇の整列。
  - Kaufman 適応移動平均の傾き（上昇/下降）。
  - Awesome Oscillator のゼロライン越え。
  - Ichimoku の転換線-基準線クロス。
- 各インジケーターは買い (+1)、売り (-1)、またはニュートラル (0) の投票を生成します。買い投票数（または売り投票数）が **MinimumConfirmations** パラメーターに達し、反対側を上回ると、戦略はその方向でポジションをオープンします。
- オプションの **CloseOnOpposite** は、反対の投票数がしきい値に達したときにポジションをクローズします。
- **リスク管理**: 銘柄の価格ステップで定義されたオプションのテイクプロフィットとストップロス。

## パラメーター

- `FastMaLength`, `SlowMaLength` – クロスオーバー移動平均の長さ。
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACDの設定。
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` – ストキャスティクス・オシレーターの設定。
- `RsiLength`, `CciLength`, `WilliamsLength` – オシレーターのルックバック。
- `BollingerLength`, `BollingerDeviation` – ボリンジャーバンド。
- `EnvelopeLength`, `EnvelopeDeviation` – SMA周辺のパーセンテージエンベロープ。
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` – Alligator SMMA。
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – Kaufman AMAの設定。
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` – Ichimokuライン。
- `AoShortPeriod`, `AoLongPeriod` – Awesome Oscillatorの期間。
- `MinimumConfirmations` – エントリーに必要な整列シグナル数。
- `AllowLong`, `AllowShort` – ロング/ショートサイドを有効化。
- `CloseOnOpposite` – 反対の投票数がしきい値を満たしたときにエグジット。
- `TakeProfitPips`, `StopLossPips` – 価格ステップ単位のオプションリスク目標（0で無効化）。
- `CandleType` – 分析用の時間軸/データタイプ。

## トレードロジックの概要

1. ローソク足が確定したときにすべてのインジケーターを更新。
2. インジケーターから強気・弱気の投票数を集計。
3. 投票数が確認しきい値に達し、反対側を上回ったときにロング/ショートでエントリー。
4. 反対側がしきい値に達したとき、オプションでフラット化。
5. 価格ステップで測定されたオプションのテイクプロフィット/ストップロスを適用。

この戦略は、VirtualTradePad のセンチメントボードを気に入っていたが、StockSharp フレームワーク内で自動化された実装を望む裁量トレーダー向けに設計されています。

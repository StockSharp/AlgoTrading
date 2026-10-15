# StockSharp API 戦略カタログ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このディレクトリには、C# と Python で実装された StockSharp API の戦略例があります。戦略フォルダーは番号範囲（`0001-0100`、`0101-0200` など）に分かれ、以下のページでは主な取引アイデア別に全戦略を分類しています。

各カタログ項目は両方の実装フォルダーへ直接リンクし、ライト・ダークテーマ対応の透明SVGロゴを使用します。

**戦略数:** 3811

**実装:** C# と Python

## 戦略タイプ

- [アービトラージ、ペア取引、相対価値 (25)](StrategyTypes/arbitrage-pairs-relative-value_ja.md) — 単一の方向予測ではなく、商品間の価格関係、スプレッド、または連動資産を取引する戦略です。
- [平均回帰と反転 (299)](StrategyTypes/mean-reversion-reversals_ja.md) — 行き過ぎた価格、勢いの枯渇、失敗したトレンドを探し、均衡または反転点への回帰を取引する逆張りシステムです。
- [ブレイクアウトとチャネル (319)](StrategyTypes/breakouts-channels_ja.md) — 価格がレンジを離れる動き、支持・抵抗の突破、または計算されたチャネル境界の通過を取引する戦略です。
- [出来高、VWAP、オーダーフロー (63)](StrategyTypes/volume-vwap-order-flow_ja.md) — 取引高、VWAP、流動性、板の厚み、またはオーダーフローを使ってエントリーと手仕舞いを判断するシステムです。
- [ローソク足と価格パターン (191)](StrategyTypes/candlestick-price-patterns_ja.md) — ローソク足形成、チャート構造、ギャップ、ピボットなど、価格動作に現れる反復パターンを認識する戦略です。
- [季節性、セッション、イベント (92)](StrategyTypes/seasonal-session-event_ja.md) — セッション、カレンダー、予定イベント、寄付きレンジ、または季節性に基づく時間依存のシステムです。
- [統計、適応モデル、AI (77)](StrategyTypes/statistical-adaptive-ai_ja.md) — 統計推定、適応モデル、機械学習、ニューラルネットワーク、またはシグナル分類を使う定量戦略です。
- [ファクター、ポートフォリオ、ローテーション (24)](StrategyTypes/factor-portfolio-rotation_ja.md) — 銘柄を順位付けし、ファクターで資金配分し、ポートフォリオを再調整または市場間でローテーションするマルチ資産手法です。
- [グリッド、DCA、ポジション管理 (143)](StrategyTypes/grid-dca-position-management_ja.md) — 注文ラダー、平均化、段階的エントリー、ポジションサイズ、手仕舞い、継続的な取引管理に重点を置く戦略です。
- [スキャルピングと執行 (133)](StrategyTypes/scalping-execution_ja.md) — エントリーのタイミング、スプレッド、注文配置、執行品質が優位性の中心となる短期システムです。
- [ボラティリティとオプション (78)](StrategyTypes/volatility-options_ja.md) — ボラティリティ局面、レンジの拡大・縮小、デリバティブ、オプション評価、ボラティリティリスクに基づく戦略です。
- [移動平均線とクロス (191)](StrategyTypes/moving-averages-crossovers_ja.md) — 移動平均線の方向、整列、シフト、リボン、および短期線と長期線のクロスを中心とするトレンドシステムです。
- [方向性トレンド指標 (264)](StrategyTypes/directional-trend-indicators_ja.md) — ADX/DMI、SuperTrend、Parabolic SAR、Ichimoku、Alligatorなどのトレンド方向・強度指標を主に使う戦略です。
- [モメンタムとオシレーターによるトレンド (206)](StrategyTypes/momentum-oscillator-trend_ja.md) — モメンタム、MACD、RSI、CCI、ストキャスティクス、ROC、ダイバージェンスなどで確認する方向性戦略です。
- [ブレイクアウト、押し戻り、プライスアクション (95)](StrategyTypes/breakouts-pullbacks-price-action_ja.md) — ブレイクアウト、押し戻り、チャネル、スイング、ローソク足、リトレースメント、市場構造によるトレンド継続エントリーです。
- [適応型・マルチタイムフレーム・特化型トレンド (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend_ja.md) — 単一の従来型指標に支配されない、適応型、マルチタイムフレーム、モデル駆動、ハイブリッド、特化型のトレンドシステムです。
- [オシレーターと指標シグナル (203)](StrategyTypes/oscillators-indicator-signals_ja.md) — 主なトリガーがオシレーター、指標のしきい値、クロス、またはダイバージェンスから得られる戦略です。
- [注文、リスク、ポジション管理 (194)](StrategyTypes/order-risk-position-management_ja.md) — 注文処理、サイズ調整、保護、グリッド、回復、トレーリング、既存ポジション管理を中心とするシステムです。
- [指標の組み合わせとシグナルロジック (319)](StrategyTypes/indicator-combinations-signal-logic_ja.md) — 指標の合意、しきい値、クロス、ダイバージェンス、シグナル選択から構成される複合エントリールールです。
- [価格水準、パターン、市場構造 (263)](StrategyTypes/price-levels-patterns-market-structure_ja.md) — 価格水準、レンジ、ピボット、フィボナッチ幾何、波動、ローソク足、市場構造に基づく特化型システムです。
- [定量・適応型・実験的戦略 (25)](StrategyTypes/quantitative-adaptive-experimental_ja.md) — 数学、統計、機械学習、適応、ランダム化、および意図的に実験的な戦略設計です。
- [ツール、パネル、アラート、テンプレート (74)](StrategyTypes/tools-panels-alerts-templates_ja.md) — 取引ユーティリティ、UIパネル、アラート、テンプレート、テスト環境、チャート補助、ライブラリ、統合例です。
- [ファンダメンタル、マクロ、資産特化型 (22)](StrategyTypes/fundamental-macro-asset-specific_ja.md) — ファンダメンタル、マクロデータ、開示情報、資産クラス、または特定の商品や市場に結び付いた専用ロジックです。
- [時間、セッション、イベントルール (13)](StrategyTypes/time-session-event-rules_ja.md) — 取引セッション、時間枠、カレンダーイベント、または反復スケジュールが主要制約となる複合戦略です。
- [方向性・ルールベース取引 (111)](StrategyTypes/directional-rule-based-trading_ja.md) — ロング・ショート、売買、トレンド、反転、エントリー、手仕舞いを明示的なルールで表す方向性システムです。
- [複合エキスパートシステム (110)](StrategyTypes/composite-expert-systems_ja.md) — 複数の仕組みを組み合わせる、多要素、ハイブリッド、アンサンブル、ロボット、トレーダー、Expert Advisorシステムです。

## リポジトリ構成

各番号付き戦略ディレクトリには概要と `CS`・`PY` 実装フォルダーがあります。タイプ別ページには短い説明とロゴによる直接リンクを含む表があります。

## 互換性

これらの例は [StockSharp API](https://github.com/StockSharp/StockSharp) 向けで、StockSharp Designer、Shell、Runner のワークフローに適応できます。

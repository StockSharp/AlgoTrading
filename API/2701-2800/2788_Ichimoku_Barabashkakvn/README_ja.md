# Ichimoku Barabashkakvn 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Vladimir Karputov の Ichimoku エキスパートアドバイザー（barabashkakvn 版）を StockSharp 高レベル API 上で再現します。クラシックな Tenkan/Kijun のクロスオーバーと Kumo クラウドからの確認を組み合わせ、MetaTrader オリジナルと同一の詳細なリスク管理を追加します。

## 機能説明

- **インジケータースタック** – 単一の Ichimoku Kinko Hyo インジケーターが Tenkan-sen、Kijun-sen、Senkou Span A、Senkou Span B の値を供給します。デフォルトの期間は 9/26/52 のままです。
- **ロングエントリー** – Tenkan が Kijun を上向きにクロスし、終値が Senkou Span B の上にある場合にトリガーされます。クロスオーバー検出は前の Tenkan 値を使用し、EA のバー対バーロジックを反映します。
- **ショートエントリー** – 終値が Senkou Span A の下にある間に Tenkan が Kijun を下向きにクロスすると出現します。
- **ポジション管理** – 1 つのネットポジションのみが維持されます。逆方向シグナルはまず既存の取引をクローズし、スクリプトの 2 段階の反転フローを再現します。
- **取引ウィンドウ** – オプションの時間フィルターにより、MQL バージョンと同じ比較を使用して、設定された開始/終了時間（含む）の間のみシステムが取引できます。

## リスク管理

- **方向別のストップとターゲット** – ロングとショートのポジションは pips 単位の独立したストップロス/テイクプロフィット距離を使用します。pips は、3 桁および 5 桁の小数点の相場に対して ×10 調整を行い、銘柄のステップサイズを使用して価格単位に変換されます。これは EA のポイント処理に一致します。
- **トレーリングストップ** – 各方向は独自のトレーリング距離と共通のトレーリングステップを持ちます。ストップは動きが `(トレーリング距離 + トレーリングステップ)` を超えた後にのみ前進します。これは元のコードとまったく同じです。
- **保護の実行** – ストップロスとテイクプロフィットのチェックは完成した各ローソク足で発生し、仮想的な保護レベルが MetaTrader のブローカー管理注文のように動作します。

## パラメーター

- `TenkanPeriod` *(デフォルト 9)* – Tenkan-sen の長さ。
- `KijunPeriod` *(デフォルト 26)* – Kijun-sen の長さ。
- `SenkouSpanBPeriod` *(デフォルト 52)* – Senkou Span B の長さ。
- `CandleType` *(デフォルト 1 時間ローソク足)* – 計算のためのデータソース。
- `OrderVolume` *(デフォルト 1 ロット)* – 取引サイズ。
- `BuyStopLossPips` / `SellStopLossPips` *(デフォルト 100)* – pips でのストップロス距離。
- `BuyTakeProfitPips` / `SellTakeProfitPips` *(デフォルト 300)* – pips でのテイクプロフィット距離。
- `BuyTrailingStopPips` / `SellTrailingStopPips` *(デフォルト 50)* – pips でのトレーリング距離。
- `TrailingStepPips` *(デフォルト 5)* – トレーリングストップをシフトするために必要な最小利益増分。
- `UseTradeHours` *(デフォルト false)* – セッションフィルターを有効にする。
- `StartHour` / `EndHour` *(デフォルト 0/23)* – 取引ウィンドウの包含的な境界（0–23）。

これらのデフォルト値は公開された EA と一致します。すべてのパラメーターは `StrategyParam<T>` オブジェクトを通じて公開されているため、ソースに触れることなく StockSharp Designer 内で最適化または調整できます。

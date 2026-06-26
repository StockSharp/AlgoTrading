# ColorMetroDuplexStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

`ColorMetroDuplexStrategy` は、MetaTrader 5のエキスパート **Exp_ColorMETRO_Duplex** をC#に変換したものです。元のロボットはColorMETROインジケーターの2つの独立したインスタンスを使用して、ロングとショートのトレーディングモジュールを管理します。各モジュールは独自のローソク足サブスクリプションで動作し、ColorMETROインジケーターが生成する2つのステップ式RSIエンベロープを評価し、高速と低速のエンベロープがクロスしたときにオプションでポジションを開閉します。

StockSharpバージョンは両モジュールを維持し、同じシグナル評価ルールを再現しながら、ローソク足サブスクリプション、注文管理、インジケーターバインディングにはハイレベルAPIを使用します。MT5のiCustom実装を模倣するカスタム `ColorMetroIndicator` が含まれており、ColorMETROの高速・低速バンドと内部RSI値を公開します。

## 動作原理

1. 2つの `SignalModule` インスタンスが作成されます — **Long** と **Short** — それぞれ独自のローソク足シリーズ、ColorMETRO設定、トレード管理オプションを持ちます。
2. 戦略が開始されると、各モジュールは設定された時間軸をサブスクライブし、`SubscribeCandles(...).BindEx(...)` を通じて `ColorMetroIndicator` をバインドします。
3. 確定したローソク足ごとにインジケーターが生成するもの：
   - 高速ColorMETROバンド（高速RSIエンベロープ）。
   - 低速ColorMETROバンド（低速RSIエンベロープ）。
   - 基礎となるRSI値（参照のみに使用）。
4. モジュールはインジケーターの履歴を保存し、設定された `SignalBar` シフトを使用して最後の2つの値を評価します（MT5の `CopyBuffer` ロジックに対応）。
5. トレードルール：
   - **ロングモジュール**
     - *開く*: 前のバーで高速バンドが低速バンドより上にあり、現在は以下または等しい。
     - *閉じる*: 前のバーで低速バンドが高速バンドより上にあった。
   - **ショートモジュール**
     - *開く*: 前のバーで高速バンドが低速バンドより下にあり、現在は以上または等しい。
     - *閉じる*: 前のバーで低速バンドが高速バンドより下にあった。
6. 注文は `BuyMarket` / `SellMarket` 経由でルーティングされます。現在のネットポジションが考慮されます — 反対のトレードは新しいポジションを開く前に既存のエクスポージャーを解消します。

## パラメーター

各モジュールは専用のパラメーターグループを公開します。デフォルト値はMT5エキスパートを反映します。

### 共有市場パラメーター

- **Long_Volume**、**Short_Volume** — 新規エントリーに使用するトレードサイズ（ロット）。
- **Long_OpenAllowed**、**Short_OpenAllowed** — モジュールのトレード開始を有効または無効にします。
- **Long_CloseAllowed**、**Short_CloseAllowed** — 自動エグジットを有効または無効にします。
- **Long_MarginMode**、**Short_MarginMode** — 互換性のために保持された資金管理モード（このポートでは効果なし）。
- **Long_StopLoss**、**Long_TakeProfit**、**Long_Deviation**、**Short_StopLoss**、**Short_TakeProfit**、**Short_Deviation** — ドキュメント用として予約済み；ストップとスリッページ制御はこのバージョンでは自動化されていません。
- **Long_Magic**、**Short_Magic** — 参照用として保存された元のMT5マジックナンバー。

### インジケーターパラメーター

- **Long_CandleType**、**Short_CandleType** — 各ColorMETROモジュールの時間軸。
- **Long_PeriodRSI**、**Short_PeriodRSI** — ColorMETROアルゴリズム内で使用されるRSI長。
- **Long_StepSizeFast**、**Short_StepSizeFast** — 高速エンベロープのステップ（RSIポイント単位）。
- **Long_StepSizeSlow**、**Short_StepSizeSlow** — 低速エンベロープのステップ。
- **Long_SignalBar**、**Short_SignalBar** — インジケーターバッファ読み取り時のバーシフト（MT5の `SignalBar` 入力と同一）。
- **Long_AppliedPrice**、**Short_AppliedPrice** — RSI計算の価格ソース（デフォルトは終値）。

## MT5との違い

- **ポジションモデル** — StockSharp戦略はネットポジションで動作します。元のエキスパートはマジックナンバーで個別ポジションを保管していましたが、ポートは反対側を開く前に現在のエクスポージャーを解消します。
- **資金管理** — マージンモードと偏差設定はパラメーターとして保持されますが、自動的には適用されません。サイズを制御するには `Volume` 入力を使用してください。
- **ストップロス / テイクプロフィット** — MT5エキスパートは各注文に保護ストップを配置していました。StockSharpバージョンは距離をパラメーターとして参照用に保持しますが、実際のストップ注文が必要な場合は別途実装する必要があります。
- **時間レベル制御** — MT5コードはグローバル変数を使用して、シグナル時間ごとに1つのトレードのみを保証していました。StockSharpでは確定したローソク足ごとに一度処理し、重複エントリーを防ぐためにネットポジションチェックに依存します。

## 注意事項

- カスタム `ColorMetroIndicator` はMT5ロジックを再現し、ステップ式RSIエンベロープとトレンドメモリを含みます。チャートやデバッグ用に高速/低速バンドと内部RSIを公開します。
- コード内のコメントは、ポーティングの決定を明確にし、さらなるカスタマイズを支援するために意図的に詳細に記述されています。
- ストップロスまたはテイクプロフィットの自動化を有効にするには、StockSharpのリスクコントロールを使用して保護注文を配置するよう `SignalModule.ProcessModule` を拡張してください。

# FatPanel ビジュアルビルダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**FatPanel ビジュアルビルダー戦略**は、MetaTraderのレガシーFAT Panel エキスパートアドバイザーのStockSharp変換版です。元のMQL実装は、ユーザーがインジケーター、ロジック、状態、および注文ブロックをリンクできるドラッグアンドドロップのキャンバスを公開していました。このC#ポートは同じモジュール哲学を維持しますが、全てのブロック接続を戦略が起動時に読み込む単一のJSONドキュメントを通じて表現します。

## 変換のしくみ

* MQLパネルはボタン、タブ、タイマーベースのディスパッチャーを作成しました。これらのUI関連は完全に削除されます。代わりに、戦略は`Configuration`パラメーター（JSON文字列）を解析し、対応するシグナルとロジックブロックを内部的にインスタンス化します。
* ブロックは設定された`CandleType`の完成した各ローソク足で評価されます。インジケーターブロックはStockSharpのインジケーター（`SMA`、`EMA`、`SMMA`、`WMA`）を使用し、手動バッファには依存しません。
* 元の注文ブロックはシンボル選択、ストップロス、テイクプロフィットを「ポイント」で許可していました。StockSharpでは、デフォルトのセキュリティは`Strategy.Security`から取得され、ストップロスとテイクプロフィットは戦略パラメーター`StopLossPoints`と`TakeProfitPoints`を通じて再導入され、`Security.PriceStep`を使用して絶対価格距離に変換されます。
* 時間と曜日の状態フィルターはMQLロジックを反映します。最善の買い値シグナルは、少なくとも1つのルールがそれを要求する場合にのみLevel1データにサブスクライブし、パネルディスパッチャーのオンデマンド更新動作を再現します。

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `CandleType` | 各シグナルを供給するデータタイプと時間軸。 |
| `Configuration` | ルール、条件、アクションを説明するJSONドキュメント。デフォルト値はパネルのEMA/SMAクロス戦略のサンプルを再現します。 |
| `Volume` | ルールがそれを上書きしない限りアクションで使用されるデフォルトの注文サイズ。 |
| `StopLossPoints` | 組み込みリスク保護のための価格ステップでの距離。ストップロスを無効にするには`0`に設定。 |
| `TakeProfitPoints` | 組み込みテイクプロフィットのための価格ステップでの距離。無効にするには`0`に設定。 |

`StopLossPoints` と `TakeProfitPoints` は、正の値が供給され**かつ**セキュリティが有効な`PriceStep`を公開している場合にのみ起動されます。

## 設定構造

JSONスキーマはFAT Panelブロック言語に近いように設計されています：

```json
{
  "rules": [
    {
      "name": "ルール名（オプション）",
      "all": [ /* 全て真でなければならない条件 */ ],
      "any": [ /* オプション条件、少なくとも1つが真でなければならない */ ],
      "none": [ /* オプション条件、全て偽でなければならない */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

各条件アイテムには次のいずれかの値を持つ`type`フィールドがあります：

| タイプ | JSONフィールド | 目的 |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | 論理演算子（`Greater`、`Less`、`Equal`、`CrossAbove`、`CrossBelow`）を通じて2つのシグナルブロックを接続します。閾値は絶対価格差として解釈されます。クロス演算子は前のローソク足が反対側にあり、現在の差が閾値を超えると発火します。 |
| `position` | `required` | FAT Panelの状態ブロックを反映します（`Any`、`FlatOnly`、`FlatOrShort`、`FlatOrLong`、`LongOnly`、`ShortOnly`）。 |
| `time` | `start`, `end` | `HH:mm`形式のイントラデイセッションフィルター。開始 > 終了はMQLパネルの夜間動作を維持します。 |
| `dayOfWeek` | `days` | 曜日名のリスト。省略すると条件はデフォルトで月曜から金曜となり、パネルのデフォルトに一致します。 |

シグナル（`left` / `right`）は次のように定義されます：

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` は`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`の手法をOHLC価格ソースのいずれかで支援します。インジケーターはパネルがチャートで選択した時間軸を使用したのと同様に、戦略のローソク足ストリームを共有します。
* `Bid` はLevel1更新からの最新の最良の買い値を使用します（相場が到着するまでローソク足のクローズにフォールバックします）。
* `Constant` はHLINEブロックを再現し、静的レベルを生成します。

ルールアクションは注文ブロックを再現します：

* `Buy` – 現在のポジションがフラットまたはショートの場合にロングポジションを開くか反転します。
* `SellShort` – ポジションがフラットまたはロングの場合にショートポジションを開くか反転します。
* `Close` – `ClosePosition()` を使用してオープンポジションをエグジットします。

アクションごとの`volume`はデフォルトの`Volume`パラメーターを上書きできます。

## 実行フロー

1. 戦略が開始すると設定JSONを解析します。無効なドキュメントは戦略を停止し、エラーログを発行します。
2. インジケーターはインスタンス化されてキャッシュされるため、複数のルールが重複計算なしに同じシグナル定義を再利用できます。
3. 完成した各ローソク足について戦略はシグナル値を更新し、次に各ルールを順に評価します。`all`条件は全て通過する必要があり、`any`は少なくとも1回通過する必要があり（提供された場合）、`none`は完全に失敗する必要があります。
4. アクションがトリガーされると戦略はルール名をログに記録し、リクエストされた成行注文を実行します。
5. オプションのストップロスとテイクプロフィットの保護は、供給されたポイント距離を使用して`OnStarted`中に一度起動されます。

## 制限と注意事項

* 主要な`Strategy.Security`のみがサポートされています。元のパネルからのクロスシンボルルーティングは複数の戦略インスタンスを必要とします。
* MQLディスパッチャーはロジックブロックの深いネスト（例：OR内のAND）を許可していました。JSON構造は`all`/`any`/`none`配列を通じて同様の制御を提供しますが、非常に複雑なグラフはまだ手動の適応が必要な場合があります。
* `Cross`演算子は最後のローソク足のみを使用します。MQLブロックはルックバックバッファと「ポイント」のデルタを公開していました。必要な感度を模倣するために`threshold`フィールドを適応させてください。
* ドラッグ位置、ダイアログウィンドウ、ツールバーアイコンなどのUI機能はStockSharpに直接相当するものがなく、意図的に省略されています。

## サンプル設定

戦略に埋め込まれたデフォルト設定は便宜のために以下に再現されます：

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

このサンプルは株式パネルテンプレートを反映しています：通常セッション中の20/50 EMA-SMA強気クロスでロングポジションを開き、逆クロスでポジションをクローズします。

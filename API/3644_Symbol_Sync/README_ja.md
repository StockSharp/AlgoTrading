# シンボル同期戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**シンボル同期戦略**は、StockSharp 環境内で MetaTrader ユーティリティ `SymbolSyncEA` を複製します。この戦略では、メイン戦略シンボルとすべての登録されたリンクされた戦略の同期が維持されます。プライマリシンボルが変更されるたびに、ストラテジーはリンクされているすべてのストラテジーに新しい証券を自動的に伝播し、手動介入なしでワークスペース全体が同じ金融商品に従うようにします。

## 核となるアイデア
- 起動時に初期戦略のセキュリティを取得し、フォールバック オプションとして再利用します。
- 常に主要なセキュリティを反映するリンクされた戦略の構成可能なリストを保持します。
- `Security` の直接割り当てまたは新しいセキュリティ識別子の指定によってトリガーされるシンボルの変更を許可します。
- 元の Expert Advisor の動作と一致するように、手動の同期とリセット操作を提供します。

## パラメーター
| 名前 | 説明 | デフォルト |
| ---- | ----------- | ------- |
| `ChartLimit` | 同期できるリンクされた戦略の最大数。誤った大量更新を防ぎます。 | `10` |
| `SyncSecurityId` | リンクされた戦略に伝播されるセキュリティの識別子。空の値は戦略セキュリティにフォールバックします。 | `""` |

## パブリックメソッド
- `RegisterLinkedStrategy(Strategy strategy)` – 戦略インスタンスを同期リストに追加します。正常に登録されると、`true` を返します。
- `UnregisterLinkedStrategy(Strategy strategy)` – リストから戦略を削除します。
- `ChangeSyncSecurity(Security security)` – 提供されたセキュリティ インスタンスに切り替え、リンクされているすべての戦略にそれを伝播します。
- `ChangeSyncSecurity(string securityId)` – 現在の `SecurityProvider` を通じて識別子を解決し、`ChangeSyncSecurity(Security)` を呼び出します。
- `ResetToInitialSecurity()` – 起動時にキャプチャされたシンボルを復元します。
- `SyncSymbols()` – 保存されている識別子を変更せずに手動で再同期を強制します。

## 使用ワークフロー
1. 戦略を開始する前に、`SymbolSyncStrategy` をインスタンス化し、プライマリ `Security` を設定するか、`SyncSecurityId` を割り当てます。
2. アクティブなシンボルをミラーリングする必要がある子戦略ごとに `RegisterLinkedStrategy` を呼び出します (たとえば、異なるタイムフレームやダッシュボード)。
3. メインシンボルを変更する必要がある場合は、`ChangeSyncSecurity(Security)` または `ChangeSyncSecurity(string)` を呼び出します。
4. 外部コンポーネントがリンクされた戦略を変更した場合は、オプションで `SyncSymbols()` を呼び出して伝播を強制します。

## MQL バージョンとの違い
- MetaTrader チャート ウィンドウではなく、StockSharp `Strategy` インスタンスで動作します。
- `SecurityProvider` 抽象化を使用して識別子を解決します。
- 防御的なログと、同期された戦略に対する構成可能な制限を追加します。
- 高度な自動化シナリオ向けに、明示的なリセットおよび手動同期方法を提供します。

## 注意事項
- この戦略は成行注文を発行しません。インフラストラクチャヘルパーとして動作します。
- プロジェクト要件に準拠するために、すべてのコード コメントは英語で保存されます。

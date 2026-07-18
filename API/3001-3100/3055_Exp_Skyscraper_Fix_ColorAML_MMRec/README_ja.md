# Exp Skyscraper Fix Color AML MMRec戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Exp Skyscraper Fix Color AML MMRecは、MQL5エキスパートアドバイザー *Exp_Skyscraper_Fix_ColorAML_MMRec* のStockSharpへのポートです。元のロボットは2つの独立したインジケーター — **Skyscraper Fix** と **Color AML** — を組み合わせ、連続損失後に注文サイズを削減するMMRecマネー管理ロジックを適用します。C#実装は両方のシグナルソースと適応的なポジションサイジングを維持しながら、注文ルーティングにStockSharpの高レベルAPIを使用します。

## トレードワークフロー

1. **Skyscraper Fixモジュール**は`SkyscraperCandleType`の完了したローソク足から適応チャンネルを構築します。チャンネルの色がtealになる（トレンド &gt; 0）と、すべてのショートポジションをクローズでき、前の色がtealでなかった場合は新しいロングトレードが開かれます。色が赤になる（トレンド &lt; 0）と、ロジックはショートトレード用に反転されます。ヘルパークラス`SkyscraperFixIndicator`は戦略`3040_Exp_Skyscraper_Fix_Duplex`から再利用されます。
2. **Color AMLモジュール**は`ColorAmlCandleType`からのローソク足を処理します。翻訳された`ColorAmlIndicator`は適応市場レベルを再現し、カラーコードを出力します：`2`（強気）、`0`（弱気）または`1`（ニュートラル）。モジュールは強気または弱気の色が検出されるたびに反対側をクローズし、色が前の遅延サンプルから変化した場合は新しいポジションを開きます。
3. **シグナル遅延**は`SkyscraperSignalBar`と`ColorAmlSignalBar`を通じて両モジュールで独立して制御されます。戦略はインジケーター出力のキューを維持し、設定された数のクローズ済みローソク足の後にのみ注文を実行し、エキスパートアドバイザーの`CopyBuffer(..., shift, ...)`動作と一致させます。
4. **リスク管理**は元のストップ/テイクプロフィット距離を反映します。各モジュールは価格ステップ（ティック）で独自の保護距離を定義します。戦略はそれらを絶対価格に変換し、完了したすべてのローソク足で、バーのレンジがストップロスまたはテイクプロフィットに触れたかどうかを確認します。そうであれば、ポジションは成行注文でフラット化され、すべての保護レベルがクリアされます。
5. **MMRecマネー管理**はSkyscraperロング、Skyscraperショート、Color AMLロング、Color AMLショートエントリーの連続損失トレードを個別に追跡します。ある方向の損失ストリークが対応するトリガー（`*LossTrigger`）に達すると、ボリュームが`*Mm`から削減値`*SmallMm`に切り替わります。利益のあるトレードが現れると、ストリークはゼロにリセットされます。サンプル戦略は単一のネットポジションで実行されるため、`Lot`管理モードのみが実際の効果を持ちます；他のモードは直接ロットサイジングにフォールバックします。

## 実装メモ

- コードはStockSharpの高レベルAPIのみに依存します：ローソク足サブスクリプションが両インジケーターを供給し、すべての取引決定は`BuyMarket`、`SellMarket`、`ClosePosition`ヘルパーを通じて実行されます。
- 保護注文は個別のストップ/リミット注文ではなく成行エグジットで実装されます。これにより、両モジュールが同じネットポジションを共有する際の競合を避けます。
- マネー管理は`OnOwnTradeReceived`で受け取った実行データを使用して前のトレードの結果を決定します。ポジションを開いたモジュールはその識別子を保存し、ポジションがクローズされたときに正しい損失カウンターが更新されます。
- 翻訳された`ColorAmlIndicator`はローソク足と平滑化値をキャッシュし、フラクタルレンジに基づくダイナミックアルファとカラーコーディングロジック（AML上昇には青、下降には赤、それ以外はグレー）を含む元の指数平滑化スキームに従います。
- MQL5バージョンのマジックナンバーと明示的なスリッページ設定はStockSharpでは不要であり、省略されています。

## パラメーター

### Skyscraper Fixモジュール

| パラメーター | デフォルト | 説明 |
| --- | --- | --- |
| `SkyscraperCandleType` | H4ローソク足 | Skyscraper Fixチャンネルの計算に使用するタイムフレーム。 |
| `SkyscraperLength` | 10 | 適応チャンネルステップを定義するATRルックバック。 |
| `SkyscraperKv` | 0.9 | ATRベースのステップサイズに適用される乗数。 |
| `SkyscraperPercentage` | 0 | 中間線に適用されるパーセントオフセット。 |
| `SkyscraperMode` | HighLow | エンベロープの価格ソース（high/lowまたはclose）。 |
| `SkyscraperSignalBar` | 1 | Skyscraperシグナルを遅延させるクローズ済みローソク足の数。 |
| `SkyscraperEnableLongEntry` | true | チャンネルが強気になったときのロングエントリーを許可。 |
| `SkyscraperEnableShortEntry` | true | チャンネルが弱気になったときのショートエントリーを許可。 |
| `SkyscraperEnableLongExit` | true | 弱気のSkyscraperシグナルでロングポジションをクローズ。 |
| `SkyscraperEnableShortExit` | true | 強気のSkyscraperシグナルでショートポジションをクローズ。 |
| `SkyscraperBuyLossTrigger` | 2 | 削減ボリュームに切り替えるために必要な連続ロング損失数。 |
| `SkyscraperSellLossTrigger` | 2 | 削減ボリュームに切り替えるために必要な連続ショート損失数。 |
| `SkyscraperSmallMm` | 0.01 | 損失トリガー到達後に使用する注文ボリューム。 |
| `SkyscraperMm` | 0.1 | Skyscraperシグナルのデフォルト注文ボリューム。 |
| `SkyscraperMmMode` | Lot | マネー管理モード（`Lot`のみがC#ポートに影響）。 |
| `SkyscraperStopLossTicks` | 1000 | 価格ステップでのストップロス距離。0でストップを無効化。 |
| `SkyscraperTakeProfitTicks` | 2000 | 価格ステップでのテイクプロフィット距離。0でターゲットを無効化。 |

### Color AMLモジュール

| パラメーター | デフォルト | 説明 |
| --- | --- | --- |
| `ColorAmlCandleType` | H4ローソク足 | Color AMLインジケーターが使用するタイムフレーム。 |
| `ColorAmlFractal` | 6 | AMLレンジ計算のフラクタルウィンドウ。 |
| `ColorAmlLag` | 7 | AML指数平均のスムージングラグ。 |
| `ColorAmlSignalBar` | 1 | Color AMLシグナルを遅延させるクローズ済みローソク足の数。 |
| `ColorAmlEnableLongEntry` | true | AMLが強気になったとき（色2）のロングエントリーを許可。 |
| `ColorAmlEnableShortEntry` | true | AMLが弱気になったとき（色0）のショートエントリーを許可。 |
| `ColorAmlEnableLongExit` | true | 弱気のAML色でロングポジションをクローズ。 |
| `ColorAmlEnableShortExit` | true | 強気のAML色でショートポジションをクローズ。 |
| `ColorAmlBuyLossTrigger` | 2 | 削減ボリュームに切り替える前の連続ロング損失数。 |
| `ColorAmlSellLossTrigger` | 2 | 削減ボリュームに切り替える前の連続ショート損失数。 |
| `ColorAmlSmallMm` | 0.01 | 損失トリガー到達後に使用する注文ボリューム。 |
| `ColorAmlMm` | 0.1 | Color AMLシグナルのデフォルト注文ボリューム。 |
| `ColorAmlMmMode` | Lot | マネー管理モード（`Lot`のみがC#ポートに影響）。 |
| `ColorAmlStopLossTicks` | 1000 | 価格ステップでのストップロス距離。無効にするには0に設定。 |
| `ColorAmlTakeProfitTicks` | 2000 | 価格ステップでのテイクプロフィット距離。無効にするには0に設定。 |

## 使用方法

1. 戦略をポートフォリオと取引したい計器にアタッチします。ローソク足のセキュリティは`SkyscraperCandleType`と`ColorAmlCandleType`で定義されたシリーズを提供する必要があります。
2. ブローカーが異なるロットステップを使用する場合は、マネー管理パラメーターを調整します。直接ロットサイジングのみが適用されるため、それに応じて`*Mm`と`*SmallMm`を設定してください。
3. オプションで各モジュールのストップロスとテイクプロフィット距離（ティック単位）を変更します。距離をゼロに設定すると対応する保護が無効になります。
4. 戦略を開始します。両方のローソク足ストリームを購読し、インジケーターを計算し、上記のルールに従って自動的にエントリーとエグジットを管理します。

READMEは`CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs`の動作を反映しており、このStockSharp実装のリファレンスドキュメントとして使用する必要があります。

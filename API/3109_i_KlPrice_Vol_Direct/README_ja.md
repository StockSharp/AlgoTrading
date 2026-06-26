# Exp i-KlPrice Vol ダイレクト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Exp i-KlPrice Vol ダイレクト戦略**はMetaTrader 5エキスパートアドバイザー`Exp_i-KlPrice_Vol_Direct`のStockSharp適応版です。
元のシステムはカスタムKlPriceオシレーターをボリュームで乗算し、複数の移動平均ステージで平滑化し、結果の線の傾きの変化に反応
します。ポートはマルチステージ処理チェーンを維持し、同じ設定可能なパラメーターを公開し、完成した足でStockSharpの高レベルAPI
を通じて取引を実行します。

MQL5バージョンから保持された主要なアイデア：
- **価格と値幅の2段階平滑化** – 価格データは設定可能な移動平均でフィルタリングされ、高-低値幅は別々に平滑化されます。
- **ボリューム重み付け** – オシレーター出力は最終Jurikフィルターの前に選択されたボリュームストリームで乗算されます。
- **方向カラーマップ** – 戦略は平滑化されたオシレーター傾きの符号を監視します。
- **シグナル遅延** – `SignalBar`でユーザーが行動前に追加の閉じた足を必要とできます。

## 処理パイプライン
1. **適用価格の選択** – MQLインジケーターと同じ12種類の適用価格フォーミュラから選択。
2. **一次平滑化** – オプションの`PricePhase`付きで`PriceLength`バーに`PriceMethod`を適用。
3. **値幅平滑化** – `RangeMethod`、`RangeLength`、`RangePhase`を使用して足の値幅（`High - Low`）に同じ手順を繰り返す。
4. **オシレーター構築** – `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50`をMQLフォーミュラと同一に計算し、
   選択されたボリュームストリーム（`VolumeSource`）で乗算。
5. **最終Jurikフィルター** – ボリューム加重オシレーターと生ボリュームストリームの両方を期間`ResultLength`のJurik移動平均
   を通じて渡す。
6. **カラー検出** – 最新の平滑化されたオシレーター値を前の値と比較。上昇値は足を強気（`0`）、下落値は弱気（`1`）、
   等しい値は前のカラーを継承。

## 取引ロジック
### ロングサイド
- **エントリー**：シグナルバー（`SignalBar`）のカラーが強気（`0`）で、直前のカラーが弱気（`1`）のとき、
  `AllowLongEntries = true`かつ現在のネットポジションが正でなければロングポジションを開く。
- **決済**：シグナルバーのカラーが強気で`AllowShortExits = true`の場合、オープンなショートポジションを閉じる。

### ショートサイド
- **エントリー**：シグナルバーのカラーが強気（`0`）の後に弱気（`1`）になったとき、`AllowShortEntries = true`かつ現在の
  ネットポジションが負でなければショートポジションを開く。
- **決済**：シグナルバーのカラーが弱気で`AllowLongExits = true`の場合、既存のロングポジションを閉じる。

## パラメーター参照
| パラメーター | 説明 | デフォルト |
|------------|------|----------|
| `CandleType` | 分析される足の時間軸。 | `H4` |
| `VolumeSource` | 重み付けに使用するボリュームストリーム（`Tick`または`Real`）。 | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | 適用価格の一次平滑化アルゴリズム、期間、Jurik位相。 | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | 足値幅の平滑化アルゴリズム、期間、位相。 | `Jjma`, `20`, `100` |
| `ResultLength` | ボリューム加重オシレーターとボリュームストリームのJurik期間。 | `20` |
| `PriceMode` | 適用価格フォーミュラ（Close、Open、Median、Demark、TrendFollow0/1など）。 | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | 視覚診断用のレベル乗数；シグナルは変更しない。 | `0`, `0`, `0`, `0` |
| `SignalBar` | カラー変化を評価する前にスキップする完全に閉じた足数。 | `1` |
| `AllowLongEntries` / `AllowShortEntries` | ロング/ショート取引を開くための許可フラグ。 | `true` |
| `AllowLongExits` / `AllowShortExits` | 反対カラーで既存ポジションを閉じるための許可フラグ。 | `true` |
| `StopLossPoints` / `TakeProfitPoints` | `StartProtection`に渡される価格ポイント単位の保護オフセット。 | `1000`, `2000` |

## リスク管理
- ストップロスとテイクプロフィットレベルは`UnitTypes.Point`オフセットに変換され、`StartProtection`で管理されます。
  それぞれの保護を無効にするにはいずれかの値を`0`に設定。
- ポジションサイズは`Strategy.Volume`で完全に制御されます。
- カラーは戦略が形成され、オンラインで、取引が許可されている場合にのみ評価されます。

## 制限とMQL5との違い
- よりエキゾチックな平滑化近似はMT5出力とわずかに異なる場合があります。
- StockSharp足は総ボリュームのみを公開します。
- 元のEAのマネー管理モードはポートされていません。
- 注文はシグナル足のクローズ直後に送られます。

# ColorMETRO Stochastic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL5エキスパート**exp_colormetro_stochastic.mq5**のC#移植版です。元のColorMETRO StochasticインジケーターをStockSharpの組み込み`StochasticOscillator`で置き換え、クロスオーバーイベントで取引します。

## ロジック
- デフォルトで8時間足をサブスクライブ（設定可能）。
- 以下のパラメーターでStochasticオシレーターを計算：
  - %K期間（`KPeriod`）
  - %D期間（`DPeriod`）
  - 追加スムージング（`Slowing`）
- クロスオーバー検出のために前の%Kと%Dの値を保存。
- %Kが%Dを上回ると**買い**。
- %Kが%Dを下回ると**売り**。
- `StartProtection`を通じてシンプルな2%ストップロスとテイクプロフィットを適用。

## パラメーター
| 名前 | 説明 |
|------|------|
| `KPeriod` | %Kラインの振り返り期間（デフォルト5）。 |
| `DPeriod` | %Dラインのスムージング期間（デフォルト3）。 |
| `Slowing` | 追加スムージング値（デフォルト3）。 |
| `CandleType` | ローソク足の時間軸、デフォルト8時間。 |

## 注意事項
元のMQLバージョンはカスタムのColorMETRO Stochasticインジケーターを高速・低速ステップラインで使用していました。この移植版は標準的なStochasticオシレーターを使用してそのシグナルを近似します。

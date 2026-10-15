# Volume Weighted MA デジタルシステム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は**Volume Weighted MA Digit System**を再現します。ローソク足の高値と安値に基づいて2つの出来高加重移動平均（VWMA）を構築します。価格がこれらのバンドをクロスすることでトレードシグナルが生成されます。

## 仕組み

1. **インジケーター**
   - `VWMA High`: ローソク足の高値に適用したVWMA。
   - `VWMA Low`: ローソク足の安値に適用したVWMA。
2. **シグナル**
   - **ロングエントリー**: 終値が`VWMA High`を上抜けする。
   - **ショートエントリー**: 終値が`VWMA Low`を下抜けする。
   - 逆方向のクロスでオープンポジションを閉じる。
3. **リスク管理**
   - 設定可能なストップロスとテイクプロフィット（ポイント）を持つ組み込みの`StartProtection`を使用。

## パラメーター

| 名前 | 説明 | デフォルト |
|------|------|----------|
| `VwmaPeriod` | VWMA計算の期間 | `12` |
| `CandleType` | 計算に使用するローソク足の時間軸 | `4h` |
| `StopLoss` | ストップロス（ポイント） | `1000` |
| `TakeProfit` | テイクプロフィット（ポイント） | `2000` |

## 注意事項

- 閉じたローソク足のみが処理されます。
- 戦略は`SubscribeCandles`、`Bind`などの高レベルAPIと標準インジケーターを使用します。
- オリジナルMQL戦略: `Exp_Volume_Weighted_MA_Digit_System.mq5`。

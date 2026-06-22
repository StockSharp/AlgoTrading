# Bill Williams Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は **Alligator** インジケーターと **Fractals** に基づいたBill Williamsのトレーディングアプローチの簡略版を実装します。

## 仕組み

- スムーズ移動平均（SMMA）を使用してAlligatorラインを計算します:
  - **Jaw** の長さ（デフォルト 13）
  - **Teeth** の長さ（デフォルト 8）
  - **Lips** の長さ（デフォルト 5）
- 完成したローソク足で強気および弱気のフラクタルを検出します。
- **買い**: 価格がAlligatorのteeth線より上にある最後の上部フラクタルを上抜けた時。
- **売り**: 価格がAlligatorのteeth線より下にある最後の下部フラクタルを下抜けた時。
- **ロング決済**: 終値がlips線を下回った時。
- **ショート決済**: 終値がlips線を上回った時。

## パラメーター

| 名前 | 説明 | デフォルト |
| ---- | ---- | ---------- |
| `JawLength` | AlligatorのJaw SMMAの期間 | 13 |
| `TeethLength` | AlligatorのTeeth SMMAの期間 | 8 |
| `LipsLength` | AlligatorのLips SMMAの期間 | 5 |
| `CandleType` | 計算に使用するローソク足タイプ | 15分足 |

すべてのパラメーターは戦略パラメーターインターフェースで最適化できます。

## 使用方法

1. ソリューションをビルドします:
   ```bash
   dotnet build
   ```
2. StockSharp環境で戦略を起動し、希望する銘柄と時間軸を選択します。

## 注記

この例はインジケーターバインディングを使用した高レベルAPIの使用方法を示しており、単純な決済以上のポジションサイジングやリスク管理は実装していません。

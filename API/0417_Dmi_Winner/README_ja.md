# DMI Winner 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DMI Winner は、Directional Movement Index（DMI）に基づくトレンドフォロー戦略です。
`+DI` ラインと `-DI` ラインがクロスし、Average Directional Index（ADX）が
キーレベルを上回ったときに取引を開始し、強いトレンドを示します。

オプションの移動平均フィルターにより、大きなトレンドの方向に沿った取引が
維持されます。ダウンサイドリスクを制限するためにストップロスも有効にできますが、
デフォルトではシステムはシグナルの逆転によってエグジットします。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: `+DI` が `-DI` を上抜け、かつ `ADX` > `KeyLevel`（オプションのMAフィルターあり）。
  - **ショート**: `-DI` が `+DI` を上抜け、かつ `ADX` > `KeyLevel`（オプションのMAフィルターあり）。
- **エグジット条件**: 反対方向のDIクロス、または有効な場合はストップロス。
- **ストップ**: オプションのストップロス（`UseSL`）。
- **デフォルト値**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: DMI, Moving Average
  - 複雑さ: 中程度
  - リスクレベル: 中

# ADX DMI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

方向性移動指数（DMI）を使用して、+DIラインと-DIラインのクロスオーバーを取引します。-DIが+DIを上回り、その後下回ると、戦略はロングポジションを開きます。+DIが-DIを上回り、その後下回ると、ショートポジションを開きます。逆シグナルはオプションで既存ポジションを決済できます。

## 詳細

- **エントリー条件**:
  - **ロング**: 前のバーで -DI が +DI を上回っており、最新バーで +DI を下回る。
  - **ショート**: 前のバーで +DI が -DI を上回っており、最新バーで -DI を下回る。
- **エグジット条件**:
  - 対応するクローズオプションが有効な場合、逆クロスオーバー。
- **インジケーター**:
  - Directional Index（デフォルト期間14）
- **ストップ**: デフォルトなし。
- **デフォルト値**:
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **フィルター**:
  - 任意の時間軸で動作
  - インジケーター: DMI
  - ストップ: 外部リスク管理によるオプション
  - 複雑さ: 基本

# DoubleUp2 CCI MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DoubleUp2 は Commodity Channel Index (CCI) と MACD を組み合わせたマーチンゲール型の戦略です。
両方のインジケーターが極端な正の値を示すとショートポジションを開き、両方が極端に負の場合はロングポジションを開きます。
負けトレードの後はポジションサイズが倍増し、以前の損失を取り戻そうとします。
利益の出たトレードは価格が固定のポイント数だけ進んだ時点で閉じられます。

## 詳細

- **エントリー条件**:
  - **ロング**: `CCI < -Threshold` かつ `MACD < -Threshold`。
  - **ショート**: `CCI > Threshold` かつ `MACD > Threshold`。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対シグナルまたは価格が利益方向に `ExitDistance` ポイント動く。
- **ストップ**: 明示的なストップロスなし。
- **デフォルト値**:
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: CCI, MACD
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高

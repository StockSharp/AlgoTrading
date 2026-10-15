# MA + BB + SuperTrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、移動平均クロスオーバーと SuperTrend の確認を組み合わせ、
Bollinger Bands をエグジットに使用します。シグナル MA がベース MA を上抜け、
かつ価格が SuperTrend ラインを上回るときにロングポジションを建てます。
ベアな SuperTrend の下で逆のクロスが発生するとショートを建てます。価格が
遠い Bollinger Band に触れるか、SuperTrend が逆方向にクロスするとポジションを
決済します。

## 詳細

- **エントリー条件**:
  - シグナル MA が SuperTrend の方向にベース MA をクロスする。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対側の Bollinger Band のタッチ、または SuperTrend の転換。
- **ストップ**: SuperTrend がトレーリングストップとして機能。
- **デフォルト値**:
  - MA シグナル長 = 89、MA 比率 = 1.08。
  - BB 長 = 30、BB 幅 = 3。
  - SuperTrend 期間 = 20、SuperTrend 係数 = 4。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MA、Bollinger Bands、SuperTrend
  - ストップ: SuperTrend
  - 複雑さ: 中程度
  - 時間軸: 短期/中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

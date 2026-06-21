# 20/200 Expertエキスパート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2つの過去バーの始値の差に基づいてトレードを開きます。shift2の始値からshift1の始値を引いた値がしきい値を超えたときにロングで入り、逆の条件でショートで入ります。ポジションは指定された時間のみ開かれ、テイクプロフィット、ストップロス、または最大保有時間後に閉じられます。

## 詳細

- **エントリー条件:**
  - ロング: open[Shift2] - open[Shift1] > DeltaLong ポイント。
  - ショート: open[Shift1] - open[Shift2] > DeltaShort ポイント。
- **ロング/ショート:** 両方。
- **エグジット条件:** テイクプロフィット、ストップロス、または最大保有時間。
- **ストップ:** ポイント単位の固定ストップロスとテイクプロフィット。
- **デフォルト値:**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 ポイント
  - DeltaShort = 21 ポイント
  - TakeProfitLong = 390 ポイント
  - StopLossLong = 1470 ポイント
  - TakeProfitShort = 320 ポイント
  - StopLossShort = 2670 ポイント
  - TradeHour = 14
  - MaxOpenTime = 504 時間
  - Volume = 0.1
  - ローソク足の時間軸 = 1時間
- **フィルター:**
  - カテゴリ: モメンタム
  - 方向: ロングとショート
  - インジケーター: なし
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: 時間足
  - 季節性: 時間ベース
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

# 強化版BarUpDn戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Bollinger Bandsとトレンド確認を組み合わせて強気または弱気のバーを探します。上昇トレンドでは強気のギャップでロング、下降トレンドでは弱気のギャップでショートにエントリーします。エグジットにはATRベースのストップロスとテイクプロフィットレベルを使用します。

## 詳細

- **エントリー条件**:
  - ロング: 上方ギャップのある強気ローソク足、トレンドMAの上かつBollinger Bandsの下限より上で終値。
  - ショート: 下方ギャップのある弱気ローソク足、トレンドMAの下かつBollinger Bandsの上限より下で終値。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 価格がATRベースのストップロスまたはテイクプロフィット（1.5× ATR）に達する。
- **ストップ**: ATRベースのストップとテイクプロフィット。
- **デフォルト値**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `MaLength` = 50
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 2
  - `AtrMultiplierTp` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger Bands, SMA, ATR
  - ストップ: あり
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

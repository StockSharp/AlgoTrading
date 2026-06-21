# 二項オプション価格付けモデル
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このモジュールは、2ステップの二項ツリーを使ってオプションの理論価格を計算します。アメリカ式またはヨーロッパ式スタイル、コールまたはプットのオプションを、さまざまなアセットクラスに対してサポートします。ボラティリティは終値の標準偏差で推定されます。

トレードシグナルは生成されません。戦略は完成した各キャンドルについて計算されたオプション価格をログに記録します。

## 詳細
- **機能**: オプション価格計算（取引なし）
- **パラメーター**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **インジケーター**: Standard Deviation
- **ロング/ショート**: N/A
- **ストップ**: なし

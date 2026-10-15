# 相対強度戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は複数の移動平均から加重相対強度指標を計算します。
強度シグナル上のBollinger Bandsが買われすぎと売られすぎのゾーンを示します。
強度が上限バンドを上抜けると買い、下限バンドを下抜けると売ります。

## 詳細

- **エントリー**: 強度が上限バンドを上抜けでロング、下限バンドを下抜けでショート。
- **エグジット**: 反対のバンドクロス。
- **インジケーター**: EMA 8、EMA 34、SMA 20、SMA 50、SMA 200、Bollinger Bands。
- **タイプ**: モメンタム。

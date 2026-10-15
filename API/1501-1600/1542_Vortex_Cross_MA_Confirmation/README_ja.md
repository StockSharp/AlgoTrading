# MA確認付きVortexクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はVortexインジケーターを使ってトレンド転換を検出し、平滑化移動平均でエントリーを確認します。プラスのVortexがマイナスのVortexを上抜けし、価格が平滑化ラインの上にあるときにロングトレードを建てます。反対のクロスでラインを下回ったときはショートトレードとなります。

## パラメーター
- **Vortex Length** – Vortex計算の期間。
- **SMA Length** – 基準SMAの長さ。
- **Smoothing Length** – 平滑化移動平均の長さ。
- **MA Type** – 平滑化手法。
- **Candle Type** – 処理するローソク足の時間軸。

# トレンドフィルターと急激な価格変動利確を組み合わせたGC戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は5/25 SMAクロスオーバーを75期間トレンドフィルターおよびADX確認と組み合わせて使用します。前日終値から指定した割合以上価格が動いた場合にポジションを決済し、急激な動きを捉えます。

## 詳細
- **エントリー**: SMA 5がSMA 25を上抜け、価格がSMA 75を上回り、ADXが閾値を超えた場合にロング。逆の条件でショート。
- **エグジット**: 逆シグナルまたは設定した割合を超える急激な価格変動。
- **インジケーター**: SMA, Average Directional Index。
- **市場**: 任意。

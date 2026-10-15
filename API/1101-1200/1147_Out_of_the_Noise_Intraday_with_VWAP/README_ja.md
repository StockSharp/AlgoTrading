# VWAPを使った "Out of the Noise" イントラデイ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

「Out of the Noise」イントラデイブレイクアウトアプローチを実装します。この戦略は、過去 *Period* 日間の平均絶対移動を使用して、セッション始値の周りに動的な上限と下限を構築します。

ロングポジションは価格が上限を上抜けたときに建て、ショートポジションは下限を下回ったときに建てます。既存のポジションは、VWAPのクロスまたは反対側の境界へのタッチでエグジットします。ポジションサイズは、オプションで日次標準偏差から導出されたボラティリティターゲットにスケーリングできます。

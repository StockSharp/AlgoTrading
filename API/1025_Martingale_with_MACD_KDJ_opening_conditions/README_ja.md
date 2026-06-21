# MACDとKDJの開始条件を持つマーチンゲール戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MACD線とKDJの%K線が同じ方向にシグナル線をクロスしたときにトレードに参入します。マーチンゲールアプローチを使ってポジションをピラミッド化し、価格が設定された割合だけトレードに逆行してから反発したときに追加します。

ポジションはテイクプロフィット、ストップロス、またはトレーリングストップ条件が満たされたときに決済されます。

## 詳細

- **エントリー**: MACD線とKDJの%K線が同じ方向にシグナル線をクロスする。
- **追加**: 価格が`Add Position Percent`動いて`Rebound Percent`反発したとき、最大`Max Additions`回まで追加。各追加サイズは`Add Multiplier`で乗算される。
- **エグジット**: `Take Profit Trigger`、`Stop Loss Percent`、またはトレーリングストップ発動時に決済。
- **方向**: ロングとショート。


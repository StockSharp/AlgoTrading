# Cnagda Fixed Swing 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashi ローソク足を使用する2つのモードを持つ戦略：
- **RSI**: 高出来高時に RSI の短期 EMA が長期 EMA をクロスした際にエントリー。
- **Scalp**: Heikin Ashi 終値の EMA と WMA のクロスオーバーに基づくエントリー。

ストップロスは直近のスイング高値または安値に設定し、テイクプロフィットは固定のリスク/リワード倍率を使用します。

## パラメーター
- ローソク足の種類
- 取引ロジック
- スイングのルックバック期間
- リスク/リワード

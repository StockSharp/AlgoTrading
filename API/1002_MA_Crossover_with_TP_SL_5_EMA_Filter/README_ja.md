# TP/SLと5 EMAフィルター付きMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

終値が5期間EMAを上回っている状態で速いSMAが遅いSMAを上抜けたときにロングエントリーします。反対の条件ではショートエントリーします。戦略はパーセンテージベースの利確と損切りレベルを使ってポジションを管理します。

## パラメーター
- Fast MA Length
- Slow MA Length
- EMA Length
- Target %
- Stop %
- Candle Type

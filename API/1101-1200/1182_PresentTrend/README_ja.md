# PresentTrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ATRベースの閾値とRSIまたはMFIを使用してトレンドの方向を追跡します。PresentTrendラインは、オシレーター値とATRに基づいて拡張または収縮することで構築されます。PresentTrendが2本前のバーの値を越えたとき、そして最新の逆シグナルが方向を確認したときにシグナルが現れます。

- **ロング**: PresentTrendが2本前のバーの値を上抜け、最後のショートシグナルが前回のロングより新しい。
- **ショート**: PresentTrendが2本前のバーの値を下抜け、最後のロングシグナルが前回のショートより新しい。
- **インジケーター**: ATR、RSIまたはMFI。
- **ストップ**: 片方向モードで逆シグナルが現れたときにポジションを決済。

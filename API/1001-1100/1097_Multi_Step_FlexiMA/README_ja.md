# マルチステップ FlexiMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrendフィルターとマルチステップ・テイクプロフィットを使用した可変長移動平均オシレーターを利用します。

- **ロング**: 価格がSuperTrendラインを上回り、オシレーターが正のとき。
- **ショート**: 価格がSuperTrendラインを下回り、オシレーターが負のとき。
- **部分決済**: 3つのテイクプロフィットレベルで段階的に決済。
- **クローズ**: 反対の条件が現れたとき、残りのポジションを決済。

**インジケーター**: 可変長SMAオシレーター、SuperTrend
**ストップ**: マルチステップ・テイクプロフィットのみ

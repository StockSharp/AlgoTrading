# Supertrend - SSL トグル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はSupertrendインジケーターとSSLチャンネルを組み合わせます。
トグルにより、トレードに入る前に両方のインジケーターからの確認を要求できます。
確認が有効な場合、最初のインジケーターのシグナルは2番目を待ってから実行されます。
どちらかのインジケーターから反対のシグナルが現れたときにポジションを閉じます。

## 詳細

- **インジケーター**: Supertrend (ATR 10, ファクター 2.4), SSLチャンネル (期間 13)
- **エントリー**: SSLクロスオーバーまたはSupertrendの方向転換（オプションの確認あり）
- **エグジット**: SSLまたはSupertrendからの反対シグナル
- **方向**: ロングとショート
- **時間軸**: 任意

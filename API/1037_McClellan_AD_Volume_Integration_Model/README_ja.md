# McClellan A-D 出来高統合モデル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、バーの価格幅に出来高を掛け合わせることで加重騰落ラインを構築します。この加重ラインの2本のEMAがMcClellanスタイルのオシレーターを形成します。

オシレーターが閾値を下回った後、ユーザー定義の閾値を上抜けたときにロングポジションが建てられます。取引は固定バー数後に自動的にクローズされます。

## 詳細

- **エントリー**: オシレーターが下から `Long Entry Threshold` を上抜ける。
- **エグジット**: `Exit After Bars` 本後にポジションをクローズ。
- **ロング/ショート**: ロングのみ。
- **インジケーター**: 2本のEMA。
- **ストップ**: なし。
- **時間軸**: 設定可能。

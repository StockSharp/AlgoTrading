# McGinley Dynamic（改良版）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

John R. McGinley, Jr. による「McGinley Dynamic (Improved)」インジケーターを実装し、終値がダイナミックラインをクロスしたときに取引します。モダン、オリジナル、カスタム係数の各フォーミュラをサポートし、比較のために非制約バリアントをオプションで表示できます。

## 詳細

- **ロングエントリー**: 終値がMcGinley Dynamicを上抜け。
- **ショートエントリー**: 終値がMcGinley Dynamicを下抜け。
- **インジケーター**: McGinley Dynamic、オプションのUnconstrained McGinley Dynamic、参照用EMA。
- **デフォルト値**: Period = 14, Formula = Modern, Custom k = 0.5, Exponent = 4.
- **方向**: 両方。

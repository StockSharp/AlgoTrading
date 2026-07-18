# TSI WPRクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はWilliams %Rオシレーターから計算されたTrue Strength Index（TSI）のクロスオーバーに基づいて取引を行います。
TSIが平滑化されたシグナルラインを上抜けすると、戦略はロングポジションに入ります。TSIがシグナルラインを下抜けすると、ショートポジションに入ります。

## パラメーター
- **Candle Type**: 計算に使用するローソク足の時間軸。
- **Williams %R Period**: Williams %Rインジケーターのバー数。
- **Short Length**: TSI計算で使用する短期EMA期間。
- **Long Length**: TSI計算で使用する長期EMA期間。
- **Signal Length**: シグナルラインを形成するためにTSIに適用するEMA期間。

## 取引ルール
1. 完了した各ローソク足のWilliams %R値を計算します。
2. この値をTrue Strength Indexインジケーターに入力します。
3. EMAでTSIを平滑化してシグナルラインを取得します。
4. TSIがシグナルラインを上抜けしたら**買い**。
5. TSIがシグナルラインを下抜けしたら**売り**。
6. 新しいシグナルが出ると反対方向の既存ポジションがクローズされます。

## 注意事項
- 戦略は自動ローソク足購読付きの高レベルAPIを使用します。
- StartProtectionは基本的なリスク管理のために起動時に開始されます。
- TSI、シグナルライン、執行済みトレードを視覚化するためにチャートエリアが作成されます。

# アラートシステム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**アラート システム** 戦略は、MetaTrader 4 エキスパート アドバイザー `AlertingSystem.mq4` を忠実に StockSharp に変換したものです。オリジナルのスクリプトは 2 本の水平線を描き、市場がそれらに触れるたびにサウンドを再生します。 The StockSharp version accomplishes the same goal by subscribing to Level1 (best bid/ask) quotes and printing journal messages when either configurable alert level is crossed.

## コアアイデア

1. レベル 1 データ ストリームを登録して、戦略がティックごとの入札と売値の更新を受信し、MQL `OnTick` ハンドラーをミラーリングします。
2. ユーザー定義の `UpperPrice` レベルと `LowerPrice` レベルを読み取ります。 `0` の値は、MetaTrader の水平線を削除するのと同じように、対応するアラートを無効にします。
3. 受信したすべての入札を上位レベルと比較し、すべての売値を下位レベルと比較します。
4. Emit a single log notification when the price crosses an active level and wait until the market returns to the safe zone before arming the alert again.これにより、元のサウンドトリガーの意図を維持しながら、騒々しい重複アラートが防止されます。

## パラメーター

| 名前 | デフォルト | 説明 |
| --- | --- | --- |
| `UpperPrice` | `0` | 水平方向の警戒レベルの上限。チェックを無効にするには、`0` に設定します。 |
| `LowerPrice` | `0` | 水平方向の警戒レベルが低くなります。チェックを無効にするには、`0` に設定します。 |

どちらのパラメータも、デザイナー UI を通じて公開されます。これらは、開始前または戦略の実行中に変更できます。次回の見積更新では新しいレベルが使用されます。

## 実行時の動作

- **データ サブスクリプション**: `GetWorkingSecurities` はレベル 1 データをリクエストし、ローソク足や取引がなくても戦略が買値/売値の更新を確実に受信できるようにします。
- **初期化**: `OnStarted` が起動すると、オペレーターが設定を確認できるように、現在構成されているレベルが戦略によってログに記録されます。
- **Alert detection**: Helper methods (`CheckUpperAlert` and `CheckLowerAlert`) store internal flags to guarantee that each breach produces exactly one notification until the market moves back beyond the threshold.
- **取引なし**: 変換では注文は送信されません。これは純粋に警告ユーティリティであり、サウンドを再生するだけの MetaTrader スクリプトの動作と一致します。
- **リセット処理**: `OnReseted` は内部フラグをクリアし、次回の実行が新しいアラート状態で開始されるようにします。

## 一般的な使用手順

1. StockSharp デザイナーで目的の楽器を選択し、`AlertingSystemStrategy` を添付します。
2. 上限および/または下限のアラート レベルを指定します。その側を無視するには、値を `0` のままにします。
3. 戦略を開始します。ログには、どのアラートがアクティブであるかを確認するエントリが表示されます。
4. ジャーナルウィンドウを監視します。ビッドが上位レベルを上回るか、アスクが下位レベルを下回ると、戦略は説明メッセージを記録します。

## 変換メモ

- 元の MetaTrader アドバイザーは、ドラッグ可能な 2 本の水平線を作成しました。 StockSharp は代わりに数値パラメータを使用します。これにより、ワークフローが決定性を保ち、アルゴリズムの実行により適したものになります。
- MetaTrader は、条件を満たすティックごとに `PlaySound` 関数をトリガーしました。ログが膨大になるのを避けるため、価格が再び許容範囲に入るまで、コンバージョンはアラートをデバウンスします。
- The logic intentionally stays indicator-free: only raw quotes are required, so the strategy works on any timeframe or instrument that provides Level1 data.

## 分類

- **カテゴリ**: ユーティリティ / アラート
- **取引方向**: なし
- **実行スタイル**: イベント駆動型の監視
- **データ要件**: レベル 1 の買値/売値
- **複雑さ**: 基本
- **推奨される期間**: 任意 (引用ベース)
- **リスク管理**: 該当なし (オープンなポジションはありません)

This documentation summarizes the StockSharp implementation and highlights the practical steps needed to reproduce the MetaTrader alerting workflow inside the platform.

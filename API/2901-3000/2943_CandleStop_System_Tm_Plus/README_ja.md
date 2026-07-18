# CandleStop システム Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

カスタムチャンネルインジケーターCandleStopを中心に構築されたブレイクアウト戦略です。システムは継続的に遅延した最高値-最高値バンドと最安値-最安値バンドを計算し、完成したローソク足がそれらのバンドを超えてクローズするのを待ち、次のバーで反応します。オプションで最大ポジション存続時間を強制し、ポイントベースの保護ストップを使用します。

## 詳細
- **エントリー条件**：前の完成したローソク足が遅延した上部チャンネル（ロング用）より上にクローズするか、遅延した下部チャンネル（ショート用）より下にクローズし、現在のバーがダブルトリガーを避けるためにチャンネル内に戻っています。
- **ロング/ショート**：独立した有効化フラグを持つロングとショートの両方の対称的なロジック。
- **エグジット条件**：反対色のCandleStopブレイクアウトが既存のポジションをクローズします；オプションの時間ベースの決済は設定された分数を超えてオープンのままの取引をクローズします。
- **ストップ**：`StartProtection` を通じた取引所ステップベースのストップロスとテイクプロフィットレベルを使用します。
- **デフォルト値**：
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5, `UpTrailShift` = 5
  - `DownTrailPeriods` = 5, `DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000, `TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = 8時間時間軸
- **フィルター**：
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: CandleStop遅延チャンネル
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 数時間
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## パラメーター
- `OrderVolume`：新規ポジションがオープンされる際の各市場エントリーの数量。
- `EnableLongEntry` / `EnableShortEntry`：新規ロングまたはショートを独立して無効にできるトグル。
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`：反対のCandleStopブレイクアウトカラーが現れた際に既存ポジションをクローズするかどうか。
- `EnableTimeExit`：最大保持時間フィルターをオンにします。
- `MaxPositionMinutes`：オープン取引が強制クローズされる前の分数；`EnableTimeExit` が真であっても無効にするにはゼロに設定。
- `UpTrailPeriods` と `UpTrailShift`：強気CandleStopチャンネルのルックバック長と後方シフト。シフトはオリジナルインジケーターのタイミングをエミュレートするためにドンチャンスタイルのバンドを数バー遅延させます。
- `DownTrailPeriods` と `DownTrailShift`：弱気チャンネルの同等パラメーター。
- `SignalBar`：ブレイクアウトカラーのために調べられるバーのインデックス（1 = 前の完成したローソク足）。次の古いバーはMQLバージョンと同様に確認に使用されます。
- `StopLossPoints` / `TakeProfitPoints`：価格ステップで表された保護ストップ距離。`StartProtection` に渡されて決済を自動管理します。
- `CandleType`：戦略に使用される主要なローソク足シリーズ。ソーススクリプトに合わせてデフォルトは8時間時間軸。

## 実装上の注意
- チャンネル値は `Highest` と `Lowest` インジケーターを `Shift` と組み合わせて計算され、オリジナルCandleStopインジケーターの遅延バンドを再現します。
- シグナルカラーはMQL戦略の `CopyBuffer` 呼び出しを模倣し、連続したローソク足での重複エントリーを避けるためにローリングバッファに格納されます。
- 注文を配置する前に、戦略は時間ベースの決済を確認し、必要に応じて反対のポジションをクローズし、設定されたボリュームを使用して新しい市場注文を発行します。

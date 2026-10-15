# ライブAlligator戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は動的なAlligator設定といくつかのEMAフィルターを使用してトレンドの反転を取引します。
AlligatorラインがL方向を変えて5つのEMAが動きを確認するときに新しいポジションを建てます。
オプションの取引時間フィルターにより選択したセッションへのエントリーを制限します。
価格が`TrailPeriod`に基づくトレーリング平滑移動平均線をクロスするとオープンポジションをクローズします。

- **エントリー条件**
  - AlligatorのLipsがJawsの上にあり、TeethがJawsの下にあり、前のバーのLipsがJawsの下にある -> 弱気トレンド後にロングを建てる。
  - AlligatorのLipsがJawsの下にあり、TeethがJawsの上にあり、前のバーのLipsがJawsの上にある -> 強気トレンド後にショートを建てる。
  - 終値、加重値、典型値、中央値、始値に対する5つのEMAがトレンドの方向に厳密に並んでいる必要がある。
- **エグジット条件**
  - 価格が`TrailPeriod`に基づくトレーリングSMMAをクロス。
  - 取引開始時にオプションのストップロスを適用。
- **使用するインジケーター**
  - AlligatorラインとトレーリングストップのSMMA。
  - 異なる価格タイプでのEMA。

パラメーターでAlligatorのベース期間、EMA確認期間、トレーリング期間、ストップロス、取引時間ウィンドウを設定できます。

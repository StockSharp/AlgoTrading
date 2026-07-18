# Volume EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は出来高の急増と商品チャネル指数（CCI）に基づいて取引します。直前のローソク足の出来高がその前のローソク足の出来高を設定可能な係数で上回ると、新しい時間の開始時にポジションを開きます。シグナルを確認するためにCCIの値が特定のバンド内に入る必要があります。

## ルール
- 同時に開かれるポジションは1つのみ。
- 毎時間の開始時：
  - **ロングエントリー** 条件：
    - 直前のローソク足が強気。
    - 直前の出来高 > 一つ前の出来高 × `Factor`。
    - CCIが`CciLevel1`と`CciLevel2`の間にある。
  - **ショートエントリー** 条件：
    - 直前のローソク足が弱気。
    - 直前の出来高 > 一つ前の出来高 × `Factor`。
    - CCIが`CciLevel4`と`CciLevel3`の間にある。
- `TrailingStop`価格ステップのトレーリングストップが利益を保護する。
- 時刻が23時になるとすべてのポジションをクローズする。

## パラメーター
- `Factor` – 出来高乗数の閾値。
- `TrailingStop` – 価格ステップ単位のトレーリング距離。
- `CciLevel1` / `CciLevel2` – ロング取引のCCI境界。
- `CciLevel3` / `CciLevel4` – ショート取引のCCI境界。
- `CandleType` – 計算に使用するローソク足の時間軸。

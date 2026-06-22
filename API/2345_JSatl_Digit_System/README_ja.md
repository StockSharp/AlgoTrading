# JSatl Digit システム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この例は、MQL5の「JSatl Digit System」エキスパートアドバイザーをStockSharpに簡略化して移植したものです。

戦略はJurik移動平均（JMA）を使ってデジタルトレンド状態を生成します：

- 終値がJMAより上の場合、状態は**上昇**になります。
- 終値がJMAより下の場合、状態は**下降**になります。

状態が上昇に変わると、パラメーターに応じてショートポジションがクローズされ、かつ/またはロングポジションが建てられます。状態が下降に変わると、ロングポジションがクローズされ、かつ/またはショートポジションが建てられます。

**パラメーター**

- `JmaLength` – JMAの期間。
- `CandleType` – 計算に使用するローソク足シリーズ。
- `StopLossPercent` – パーセント単位の保護的ストップロス。
- `TakeProfitPercent` – パーセント単位の保護的テイクプロフィット。
- `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – 対応するシグナルに対するアクションを有効/無効にする。

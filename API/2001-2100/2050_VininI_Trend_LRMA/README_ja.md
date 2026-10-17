# VininI Trend LRMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

確定終値の移動線形回帰の終点 `L` を追跡します。既定値は `CandleType = 4時間`、`Period = 13`、`UpLevel = 10`、`DnLevel = -10`、`Mode = Breakdown`。指標形成と売買許可を待ちます。

**Breakdown** の閾値は相対オシレーター `O = 100 × (Close − L) / L` の百分率です。`previousO <= UpLevel && O > UpLevel` で買い、`previousO >= DnLevel && O < DnLevel` で売ります。LRMA の絶対価格水準ではありません。

**Twist** は回帰終点の傾きが負方向から正方向になれば買い、逆なら売ります。横ばい区間は直前のゼロでない方向を保持します。

買いは `Position <= 0`、売りは `Position >= 0` で許可します。反対の約定済み建玉を成行で閉じてから、新方向に `Volume` を建てます。同方向シグナルで積み増しません。損切りや利確はありません。

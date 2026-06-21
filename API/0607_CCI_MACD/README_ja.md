# CCI MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

CCIのゼロラインクロスとMACDフィルター、EMA/ATRバンドを組み合わせてトレンド方向に取引します。

## 詳細

- **データ**: 価格ローソク足。
- **エントリー**: CCIがゼロを上抜け、MACDがゼロ以上、価格がEMA125とEMA750を上回るがATR上限バンドを下回る場合にロング；その逆の条件でショート。
- **エグジット**: 逆シグナルでポジションをクローズ。
- **銘柄**: あらゆる銘柄。
- **リスク**: ストップロスおよびテイクプロフィットなし。

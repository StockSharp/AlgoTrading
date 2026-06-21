# MA2CCI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は速い・遅い単純移動平均線（SMA）のクロスオーバーとCommodity Channel Index（CCI）を確認フィルターとして組み合わせます。移動平均線とCCIの両方が同じ方向にそれぞれのレベルをクロスした時のみポジションを建てます。Average True Range（ATR）が初期ストップロス距離を定義します。

システムは両方向で取引できます。テイクプロフィットはなく、逆シグナルまたはATRベースのストップロス発動時にポジションをクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いSMAが遅いSMAを上抜け **かつ** CCIが0を上抜け。
  - **ショート**: 速いSMAが遅いSMAを下抜け **かつ** CCIが0を下抜け。
- **エグジット条件**:
  - 逆方向のSMAクロスオーバー。
  - ATRベースのストップロス。
- **インジケーター**: SMA、CCI、ATR。
- **時間軸**: `CandleType`で設定可能。
- **デフォルトパラメーター**:
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **ロング/ショート**: 両方。
- **ストップ**: あり、ATRを使用した動的ストップ。

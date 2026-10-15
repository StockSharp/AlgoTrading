# Price Action戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Price Action戦略**は、前のポジションが決済されるたびにロングとショートの成行注文を交互に出します。
固定のストップロス距離、レバレッジベースのテイクプロフィット目標、および設定可能なステップで市場についていくオプションのトレーリングストップを適用します。

## 詳細
- **エントリー条件:** オープンポジションなし。方向は各取引後に買いと売りの間で切り替わります。
- **ロング/ショート:** 両方。
- **エグジット条件:** 価格がトレーリングストップ、初期ストップ、またはテイクプロフィットレベルに到達した場合。
- **ストップ:** オプションのトレーリング付き固定ストップ距離（ステップは更新のための最小価格移動を定義します）。
- **デフォルト値:** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`.

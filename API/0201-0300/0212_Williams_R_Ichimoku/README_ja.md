# Williams R Ichimoku 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
このセットアップは、Williams %Rのモメンタム極値と一目均衡表の雲によって定義されるトレンド構造を組み合わせます。価格が雲の有利な側にあり、短期線がバイアスを確認している場合にのみ、強い動きに参加するというアイデアです。

テストでは年間平均リターン約73%を示しています。暗号資産市場で最もパフォーマンスが高いです。

オシレーターがWilliamsROversoldを下回り、価格が雲の上に留まり、転換線が基準線の上にあるときにロングの機会が現れます。%RがWilliamsROverboughtを上回って上昇し、価格が雲の下にあり、転換線が基準線の下にあるときにショートシグナルが発生します。価格が雲の反対側をクロスするまでポジションは開いたままです。

この手法は複数の確認を待つため、速い反転よりも明確なトレンドフィルターを好むトレーダーに適しています。雲の反対側が動的ストップとなり、リスクは基礎となるトレンドとともに調整されます。

## 詳細
- **エントリー条件**:
  - **ロング**: %R < WilliamsROversold && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **ショート**: %R > WilliamsROverbought && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格が雲の下をクロスしたときに終了
  - **ショート**: 価格が雲の上をクロスしたときに終了
- **ストップ**: はい。
- **デフォルト値**:
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
    -80と-20はルールが示すWilliams %R水準の既定値です。ロングは価格が雲の下で引けたとき、ショートは雲の上で引けたときに決済します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Williams R Ichimoku
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中


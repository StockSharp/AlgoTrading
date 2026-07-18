# モメンタム Keltner Stochastic コンボ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

モメンタム比較とKeltnerベースのStochasticオシレーターを組み合わせた戦略です。  
ポジションサイズは資本に基づいて動的にスケーリングされ、固定ストップロスで保護されます。

## 詳細

- **エントリー条件**:  
  - ロング: `Momentum > 0` かつ `KeltnerStoch < Threshold`  
  - ショート: `Momentum < 0` かつ `KeltnerStoch > Threshold`
- **ロング/ショート**: 両方  
- **エグジット条件**:  
  - ロング: `KeltnerStoch > Threshold`  
  - ショート: `KeltnerStoch < Threshold`
- **ストップ**: エントリーの下/上に固定`SlPoints`  
- **デフォルト値**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:  
  - カテゴリ: トレンドフォロー  
  - 方向: 両方  
  - インジケーター: Momentum、EMA、ATR  
  - ストップ: はい  
  - 複雑さ: 中級  
  - 時間軸: 中期  
  - 季節性: いいえ  
  - ニューラルネットワーク: いいえ  
  - ダイバージェンス: いいえ  
  - リスクレベル: 中

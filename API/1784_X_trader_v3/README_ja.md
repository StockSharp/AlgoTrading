# X Trader V3 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2本の中値移動平均線のクロスオーバーを取引します。最初の移動平均線は長く、シフトされており、2番目は短いものです。最初の移動平均線が2番目の移動平均線を下抜けし、2バー前は上にいた後、2バー間下に留まった場合にロングポジションを開きます。逆のクロスオーバーではショートポジションを開きます。逆シグナルでポジションを閉じることができます。取引は特定のイントラデイ時間ウィンドウに制限されます。オプションの保護ストップが利用可能です。

## 詳細

- **エントリー条件**:
  - 中値SMA(`Ma1Period`)が中値SMA(`Ma2Period`)を下抜けし2バー間下に留まる ⇒ `AllowBuy` が true のとき買い。
  - 中値SMA(`Ma1Period`)が中値SMA(`Ma2Period`)を上抜けし2バー間上に留まる ⇒ `AllowSell` が true のとき売り。
  - ローソク足の時刻が `StartTime` と `EndTime` の間。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - `CloseOnReverseSignal` が true のとき逆クロスオーバーで決済。
- **ストップ**:
  - `TakeProfitTicks` と `StopLossTicks` によるオプションのテイクプロフィットとストップロス（ティック単位）。
- **デフォルト値**:
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **フィルター**:
  - カテゴリ: クロスオーバー
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: オプション
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

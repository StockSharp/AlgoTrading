# Virtual Profit/Loss Trail戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

`VirtualProfitLossTrailStrategy` は、MetaTrader エキスパートアドバイザー "Virtual Profit Loss Trail" の動作を StockSharp 内で再現します。この戦略は自分で新規ポジションを開きません。代わりに、選択された銘柄の現在ポジションを継続的に監視し、保護ロジックを適用します。

- pips で表される設定可能な take-profit 距離。
- pips で表される設定可能な stop-loss 距離。
- 最小利益に到達した後に有効になり、価格が指定された trailing step だけ進んだ場合にのみ市場とともに動く仮想 trailing stop。

保護水準は仮想であるため、実際の stop 注文や limit 注文は取引所へ送信されません。戦略は最良 bid/ask 更新を監視し、仮想水準のいずれかに触れると、成行注文でオープンポジションを閉じます。

## パラメーター

| パラメーター | 説明 |
|-----------|------|
| **Take-profit (pips)** | エントリー価格と利益目標の距離。take-profit エグジットを無効にするには `0` に設定します。 |
| **Stop-loss (pips)** | エントリー価格と保護ストップの距離。stop-loss エグジットを無効にするには `0` に設定します。 |
| **Trailing stop (pips)** | trailing stop の計算に使用する距離。`0` に設定すると trailing ロジックは完全に無効になります。 |
| **Trailing step (pips)** | trailing stop をさらに移動する前に必要な追加利益。新しい高値/安値が出るたびに trail を動かすには `0` を使用します。 |
| **Trailing activation (pips)** | trailing stop が有効になる前に固定する必要がある最小利益。`0` に設定すると、ポジションに入った直後から trailing が開始されます。 |

すべての距離は pip 単位で測定されます。戦略は銘柄の価格ステップから pip サイズを自動的に導出します。小数 3 桁または 5 桁のシンボルでは 1 pip は 10 価格ステップ、それ以外では 1 ステップとして定義されます。

## ロジック

1. **市場データ購読** - 戦略は Level1 データを購読し、最良 bid と最良 ask の更新を受け取ります。完了した更新だけを処理するため、リアルタイムと履歴リプレイの両方で動作します。
2. **ロングポジション管理** - ネットポジションがロングの場合、戦略は平均エントリー価格に基づいて仮想 stop-loss、take-profit、trailing stop 水準を計算します。最良 bid が stop-loss または take-profit に触れると、ポジションを直ちに閉じます。起動利益に達すると、trailing stop は価格を上方向へ追随します。trailing step 要件が満たされた場合にのみ stop が進みます。
3. **ショートポジション管理** - 同じロジックを対称的に適用し、ショートポジションのエグジットには最良 ask を使用します。
4. **リセット動作** - ポジションが完全に閉じられると、偶発的な再エントリーシグナルを防ぐため、内部 trailing 参照がリセットされます。

## 使用のヒント

- すでにオープンポジションがある、または他の戦略や手動取引から注文を受けるコネクターと銘柄に戦略を接続します。マネージャーは集約ポジションサイズを制御します。
- Level1 データが利用可能であることを確認してください。現在の bid/ask 値がないと、仮想水準を評価できません。
- 同じポートフォリオと銘柄の下で実行することで、任意のエントリー生成戦略と組み合わせられます。競合を避けるため、保護ロジックを管理するインスタンスは 1 つだけにしてください。

## MQLエキスパートとの違い

- StockSharp 版は個別注文 ticket ではなく集約ポジションで動作します。プラットフォームが提供する平均エントリー価格を自動計算します。
- 元エキスパートの視覚的なライン描画と音声アラートは、StockSharp 内の logging に置き換えられます。保護アクションは戦略ジャーナルで確認できます。
- trailing activation しきい値と増分 trailing step を含む、同じ pip ベース設定が維持されています。

## ファイル

- `CS/VirtualProfitLossTrailStrategy.cs` - 戦略の C# 実装。
- `README.md` - このドキュメント。
- `README_zh.md` - 簡体字中国語訳。
- `README_ru.md` - ロシア語訳。

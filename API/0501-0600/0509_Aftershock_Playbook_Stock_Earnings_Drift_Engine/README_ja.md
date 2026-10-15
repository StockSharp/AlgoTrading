# Aftershock Playbook 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Aftershock Playbook** 戦略は、1 本のローソク足における異常に大きな価格変動を決算サプライズの代理シグナルとみなし、その後のドリフトを追います。市場のローソク足だけを使用し、外部の決算データフィードは必要ありません。

- **シグナル**: 完了した各 `CandleType` ローソク足で、終値間の変化を `AtrLength` 期間で計算した ATR と比較します。
- **エントリーまたは反転**: `ATR × SurpriseThreshold` を超える上昇でロングを開始またはロングへ反転し、同等の下落でショートを開始またはショートへ反転します。
- **エグジット**: `ATR × AtrMultiplier` を超える不利な動きで現在のポジションを閉じます。同じ動きがエントリー閾値にも達した場合は、ポジション反転が優先されます。
- **クールダウン**: エントリー、反転、またはエグジット後、完了した `CooldownBars` 本のローソク足にわたり、すべてのシグナルを無視します。

# Beta Adjusted Pairs Trading

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reversão à média neutra em beta de dois instrumentos. É obrigatório fornecer `Asset2` diferente e `Asset2Portfolio` explícito, que pode ser o principal. Sem TON ou conta implícitos; ambas as betas positivas. Apenas velas concluídas de `OpenTime` idêntico são combinadas; dados ausentes, antigos ou duplicados não misturam horários.

## Regras de negociação

`h=BetaAsset1/BetaAsset2`; `B1,B2` são os primeiros fechamentos positivos pareados. Spread `s=ln(P1/B1)-h*ln(P2/B2)`. Média e desvio populacional dos últimos `LookbackPeriod` spreads, incluindo o atual: `z=(s-mean)/deviation`. Desvio zero bloqueia entrada. Compra do par: `z[p]<=-EntryThreshold && -EntryThreshold<z<0`; venda: `z[p]>=EntryThreshold && 0<z<EntryThreshold`. Apenas permanecer além do limite não basta.

Compra adquire `Volume` do principal e vende `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2` do segundo; venda inverte as duas pernas. Preços das velas pareadas de sinal; limites de lote do hedge são validados e a quantidade permanece fixa. Entrada exige ambas as posições zeradas, nenhuma ordem pendente e intervalo de índices estritamente maior que `CooldownBars`.

A cada par concluído, preços médios executados `F1,F2` e quantidades com sinal `q1,q2`: `PnL=q1*(P1-F1)+q2*(P2-F2)`, `G=abs(q1)*F1+abs(q2)*F2`. Fecham-se ambas as posições reais se `PnL<=-G*StopLoss/100`; caso contrário, compra sai em `z>=-ExitThreshold`, venda em `z<=ExitThreshold`. Perda tem prioridade; espera de entrada não atrasa saída. É limite de perda do par no fechamento antes de taxas, não stop intravela da bolsa.

As pernas são ordens a mercado separadas, não atômicas. Pendentes bloqueiam decisões. Após finalizarem, perna ausente, rejeitada ou incompleta leva ao fechamento dos resíduos reais no próximo par coincidente; saídas que falharem são repetidas nos próximos pares. O arquivo BTC/TON testa mecânica, não rentabilidade nem relação econômica estável.

As entradas exigem velas concluídas e indicadores aquecidos. A posição é acompanhada até a saída; ordens a mercado pendentes impedem envios duplicados.

## Parâmetros

| Parameter | Default |
|---|---|
| `Asset2` | `required` |
| `Asset2Portfolio` | `required` |
| `BetaAsset1` | `1` |
| `BetaAsset2` | `1` |
| `LookbackPeriod` | `20` |
| `EntryThreshold` | `2` |
| `ExitThreshold` | `0.15` |
| `StopLoss` | `2 (%)` |
| `CooldownBars` | `120` |
| `CandleType` | `5m` |

# Estratégia Aftershock Playbook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Aftershock Playbook** interpreta um movimento de preço excepcionalmente grande em uma única vela como aproximação de uma surpresa de resultados e acompanha a deriva subsequente. Ela usa apenas velas de mercado e não requer uma fonte externa de resultados.

- **Sinal**: Em cada vela `CandleType` concluída, a variação entre fechamentos é comparada ao ATR calculado durante `AtrLength` períodos.
- **Entrada ou reversão**: Uma alta superior a `ATR × SurpriseThreshold` abre ou reverte para uma posição comprada; uma queda equivalente abre ou reverte para uma posição vendida.
- **Saída**: Um movimento adverso superior a `ATR × AtrMultiplier` fecha a posição atual. Se o movimento também atingir o limiar de entrada, a reversão tem prioridade.
- **Intervalo**: Após uma entrada, reversão ou saída, todos os sinais são ignorados durante `CooldownBars` velas concluídas.

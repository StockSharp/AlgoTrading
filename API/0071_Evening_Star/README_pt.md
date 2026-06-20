# Estratégia de Padrão Estrela da Tarde
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Estrela da Tarde é o espelho da Estrela da Manhã, mas indica um potencial topo. Começa com uma forte vela altista, seguida de uma pequena vela de indecisão, e termina com uma vela baixista fechando abaixo do ponto médio da primeira barra.

Os testes indicam um retorno anual médio de aproximadamente 100%. Tem melhor desempenho no mercado forex.

O algoritmo observa sequências de três velas. Quando o padrão se forma, entra vendido com um stop acima da máxima da pequena vela do meio. As posições saem quando o preço cai abaixo da mínima da vela de confirmação ou se o stop for acionado.

Como o setup antecipa uma reversão rápida de condições de sobrecompra, as operações geralmente visam movimentos curtos e impulsionados pelo momentum de queda.

## Detalhes

- **Critérios de entrada**: Padrão de três velas Estrela da Tarde.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: Preço abaixo da mínima da barra de confirmação ou stop-loss.
- **Stops**: Sim, acima da máxima da vela do meio.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoria: Padrão
  - Direção: Vendido
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


# Estratégia Universal de Stop Móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a ideia central do script MQL4 original `cm_universal_trailing_stop.mq4`. Ela não gera sinais de entrada; em vez disso, gerencia uma posição existente movendo o stop-loss na direção do lucro.

O algoritmo mantém um deslocamento do preço atual e desloca o stop sempre que o mercado se move em um passo configurável. Uma vez que o limite mínimo de lucro é atingido, o trailing stop fica ativo e segue o preço automaticamente tanto para posições compradas quanto vendidas.

## Detalhes

- **Critérios de entrada**: nenhum. A posição deve ser aberta manualmente ou por outra estratégia.
- **Comprado/Vendido**: ambos.
- **Critérios de saída**: ordem de stop acionada quando o preço recua pelo deslocamento configurado.
- **Stops**: trailing stop baseado em pontos.
- **Parâmetros**:
  - `Delta` – distância do preço ao stop em pontos.
  - `Step` – movimento mínimo de preço em pontos para deslocar o stop.
  - `StartProfit` – lucro em pontos necessário para ativar o trailing.
  - `CandleType` – período usado para os cálculos de trailing.
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Trailing
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

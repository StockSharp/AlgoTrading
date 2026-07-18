# PZ Reversão com Seguidor de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue rompimentos de máximas e mínimas de longo prazo. Compra quando o preço de fechamento supera a máxima mais alta do período de retrospectiva e vende a descoberto quando o preço de fechamento cai abaixo da mínima mais baixa. A posição é sempre revertida em sinais opostos, mantendo a estratégia continuamente no mercado.

A abordagem tenta capturar tendências sustentadas entrando após um rompimento significativo. Como o sistema opera apenas em extremos importantes, pode evitar ruído menor, mas pode incorrer em grandes rebaixamentos durante condições voláteis.

## Detalhes

- **Critérios de entrada**: Rompimento da máxima/mínima das `Period` barras anteriores.
- **Comprado/Vendido**: Ambas as direções, sempre no mercado.
- **Critérios de saída**: Sinal de rompimento oposto.
- **Stops**: Não
- **Valores padrão**:
  - `Period` = 100
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

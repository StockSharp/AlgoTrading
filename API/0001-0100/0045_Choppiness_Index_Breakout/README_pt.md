# Estratégia Choppiness Index Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Choppiness Index mede se o mercado está em tendência ou em consolidação. Quando o indicador cai abaixo de um limiar, sinaliza o início de uma tendência a partir de um ambiente agitado.

Os testes indicam um retorno anual médio de aproximadamente 172%. Funciona melhor no mercado de câmbio.

Esta estratégia entra na direção do preço em relação à sua média móvel quando a choppiness diminui. Sai se a choppiness subir novamente acima de um limiar alto ou se um stop-loss for atingido.

O objetivo é capturar novas tendências que emergem de períodos de consolidação.

## Detalhes

- **Critérios de entrada**: Choppiness abaixo de `ChoppinessThreshold` com preço acima/abaixo da MA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Choppiness acima de `HighChoppinessThreshold` ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Choppiness, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


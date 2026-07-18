# Estratégia RSI + 1200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia RSI + 1200** busca capturar reversões de tendência confirmadas por
força relativa e um filtro de tendência de período superior. Combina um Relative
Strength Index clássico de 14 períodos com uma Média Móvel Exponencial calculada
em uma série de múltiplos períodos de 120 minutos ("1200" refere-se ao período superior
no conceito original). Os sinais de trading só são capturados quando o momentum e o
filtro de tendência se alinham.

Backtests em pares de criptomoedas líquidas mostram que o método funciona melhor em
mercados direcionais sustentados. Períodos agitados ou de consolidação podem produzir sinais falsos,
portanto a estratégia inclui uma pequena folga de preço ao redor da EMA e um
stop‑loss baseado em percentual para ajudar a gerenciar o risco.

Uma operação comprada é aberta quando RSI cruza para cima a partir do território de sobrevenda e
o preço está dentro de um por cento acima da EMA do período superior. A configuração vendida é
a condição espelhada. As posições são fechadas quando RSI atinge o extremo oposto, sinalizando
o esgotamento do movimento. Um stop protetor também é colocado a
`stopLossPercent` por cento do preço de entrada.

## Detalhes

- **Condições de entrada**
  - **Comprado**: RSI cruza acima de `rsiOversold` e o fechamento é <= 1% acima da EMA.
  - **Vendido**: RSI cruza abaixo de `rsiOverbought` e o fechamento é >= 1% abaixo da EMA.
- **Condições de saída**
  - **Comprado**: RSI sobe acima de `rsiOverbought`.
  - **Vendido**: RSI cai abaixo de `rsiOversold`.
- **Stops**: Stop‑loss percentual opcional via `stopLossPercent`.
- **Parâmetros padrão**
  - `rsiLength` = 14
  - `rsiOverbought` = 72
  - `rsiOversold` = 28
  - `emaLength` = 150
  - `mtfTimeframe` = 120 minutos
  - `stopLossPercent` = 0.10 (10%)
- **Filtros**
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RSI, EMA
  - Stops: Sim
  - Complexidade: Médio
  - Período: Intradiário / multi‑período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado

# Estratégia do Sistema de Impulso Elder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o Sistema de Impulso Elder que combina a direção de uma Média Móvel Exponencial (EMA) com o momentum do histograma MACD. Ela abre negociações quando o impulso altista ou baixista desaparece em candles de períodos superiores.

A abordagem observa impulsos codificados por cores derivados da inclinação da EMA e da dinâmica do histograma MACD:
- **Verde (2)** — EMA subindo e o histograma MACD subindo e positivo.
- **Vermelho (1)** — EMA caindo e o histograma MACD caindo e negativo.
- **Azul (0)** — qualquer outro estado.

Uma posição comprada é aberta quando um impulso altista anterior (verde) enfraquece, enquanto as posições vendidas aparecem após o enfraquecimento de um impulso baixista (vermelho). As posições opostas são fechadas quando o impulso correspondente é detectado.

## Detalhes

- **Critérios de entrada**: Mudança de cor do Elder Impulse em candles finalizados.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Impulso oposto ou proteção de posição.
- **Stops**: Usa `StartProtection` com stop e take profit de 2% por padrão.
- **Valores padrão**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: EMA, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

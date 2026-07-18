# Estratégia de Reversão Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que busca formações clássicas de padrões de candles de reversão altista. Quando qualquer um desses padrões aparece abaixo de uma média móvel simples de 50 períodos, a estratégia abre uma posição comprada. Um trailing stop opcional protege os lucros abertos.

## Padrões
- **Abandoned Baby** – dois candles de baixa consecutivos seguidos de um candle de alta que fecha acima da abertura do primeiro candle, enquanto o segundo candle abre com gap para baixo.
- **Morning Doji Star** – um candle de baixa, um doji ou candle de corpo pequeno, e então um candle de alta fechando de volta ao corpo do primeiro candle.
- **Three Inside Up** – um candle de baixa, um candle de alta menor dentro do seu intervalo, e então um candle de alta fechando acima da abertura do primeiro candle.
- **Three Outside Up** – um candle de baixa seguido de um candle de alta maior que o engloba e um terceiro candle de alta confirmando a reversão.
- **Three White Soldiers** – três candles de alta consecutivos com fechamentos crescentes.

## Detalhes
- **Critérios de entrada**: qualquer padrão listado e o último candle abriu abaixo da média móvel
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: trailing stop opcional
- **Stops**: Trailing
- **Valores padrão**:
  - `MaPeriod` = 50
  - `TrailingStop` = 50
  - `UseTrailingStop` = true
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Trailing
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

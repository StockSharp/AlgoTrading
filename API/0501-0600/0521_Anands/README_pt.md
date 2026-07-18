# Estratégia Anands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema de rompimento define a direção da operação usando o candle do dia anterior.
Se o fechamento anterior estiver acima da máxima daquele dia, a estratégia busca compras; um fechamento abaixo da mínima a torna baixista.
No período de 15 minutos, observa os dois últimos candles concluídos.
Uma posição comprada é aberta quando o candle anterior fecha acima da máxima de duas barras atrás.
Uma posição vendida é aberta quando o fechamento anterior cai abaixo da mínima de duas barras atrás.

## Detalhes

- **Critérios de entrada**:
  - Fechamento do dia anterior acima/abaixo de seu intervalo define viés altista/baixista.
  - **Comprado**: fechamento anterior de 15m > máxima de duas barras atrás.
  - **Vendido**: fechamento anterior de 15m < mínima de duas barras atrás.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Não definidos, sinal inverso fecha.
- **Stops**: Sugerido no lado oposto da barra de rompimento.
- **Valores padrão**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Candles
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia de Candle Envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera sobre um padrão envolvente selecionado. Escolha entre envolvente de alta ou de baixa e um lado da operação para abrir quando o padrão aparecer. A posição é mantida por um número fixo de barras antes de ser encerrada.

## Detalhes

- **Critérios de entrada**: Padrão envolvente selecionado (de alta ou de baixa).
- **Comprado/Vendido**: Comprado ou vendido configurável.
- **Critérios de saída**: Posição encerrada após o número especificado de barras.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `HoldPeriods` = 17
  - `Pattern` = Bullish
  - `Side` = Long
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

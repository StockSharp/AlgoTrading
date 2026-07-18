# Estratégia de Trading de Precisão: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de scalping para o Ouro alinha um cruzamento de EMA rápida e EMA lenta com a direção de uma Hull Moving Average. As operações ocorrem apenas quando o RSI confirma o momentum e a volatilidade é adequada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta, RSI > 55, HMA em alta, filtro de volatilidade passa.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta, RSI < 45, HMA em queda, filtro de volatilidade passa.
- **Indicadores**: EMA, HMA, RSI, ATR, Highest/Lowest.
- **Tipo**: Seguidor de tendência.
- **Período**: Curto prazo.

# Estratégia de Tendência de Mercado por Níveis Sem Repintura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA que opcionalmente filtra negociações usando RSI. Posições compradas são abertas quando a EMA rápida cruza acima da EMA lenta, enquanto negociações vendidas são acionadas no cruzamento oposto. Quando `ApplyExitFilters` está habilitado e o filtro RSI está ativo, as posições são fechadas se o RSI sair da zona permitida.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Fast EMA` cruza acima de `Slow EMA` e `RSI > RsiLongThreshold` quando habilitado
  - **Vendido**: `Fast EMA` cruza abaixo de `Slow EMA` e `RSI < RsiShortThreshold` quando habilitado
- **Critérios de saída**: Cruzamento oposto ou falha do filtro RSI quando `ApplyExitFilters` é verdadeiro
- **Tipo**: Seguidor de tendência
- **Indicadores**: EMA, RSI
- **Período**: 5 minutos (padrão)

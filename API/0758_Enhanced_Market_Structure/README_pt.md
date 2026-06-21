# Estratégia Aprimorada de Estrutura de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estrutura de Mercado Aprimorada combina análise de máximas e mínimas de swing com filtros ATR, RSI, volume, MACD e EMA. A estratégia entra em rompimentos ou reversões de varredura quando múltiplos filtros confirmam o momentum.

## Detalhes

- **Critérios de entrada**: rompimento ou varredura de swing recente com filtros
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ATR, RSI, MACD, EMA, Volume
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio


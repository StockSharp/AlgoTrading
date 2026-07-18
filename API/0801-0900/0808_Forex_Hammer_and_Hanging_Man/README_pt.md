# Estratégia Forex de Martelo e Enforcado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera os padrões clássicos de reversão de candlestick: o martelo altista e o enforcado baixista. Entra comprado após um martelo e vendido após um enforcado, mantendo a posição por um número fixo de barras.

A posição é fechada assim que o período de manutenção expira ou os stops de proteção são atingidos.

## Detalhes

- **Critérios de entrada**: Martelo para comprado, enforcado para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Período de manutenção ou stop-loss/take-profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `BodyLengthMultiplier` = 5
  - `ShadowRatio` = 1
  - `HoldPeriods` = 26
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

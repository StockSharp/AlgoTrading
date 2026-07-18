# Estratégia de Vela de Movimento Dirigido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia monitora o Índice de Força Relativa (RSI) nos fechamentos de velas. Quando o RSI sai da zona neutra e cruza níveis definidos pelo usuário, a estratégia abre posições na direção do momentum e fecha qualquer exposição oposta.

## Detalhes

- **Indicador**: Índice de Força Relativa com `RsiPeriod` ajustável.
- **HighLevel**: valor do RSI indicando momentum de alta.
- **MiddleLevel**: limiar neutro mantido como referência.
- **LowLevel**: valor do RSI indicando momentum de baixa.
- **Entrada**:
  - Comprado quando o RSI sobe acima de `HighLevel` após ter estado abaixo.
  - Vendido quando o RSI cai abaixo de `LowLevel` após ter estado acima.
- **Saída**: O sinal oposto fecha a posição existente antes de abrir uma nova.
- **Comprado/Vendido**: Ambas as direções.
- **Stops**: Não utilizados por padrão.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = período de 5 minutos

# Oscilador Delta-RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o oscilador Delta-RSI, definido como a variação do RSI suavizada com uma EMA. Os sinais são acionados quando o delta cruza o zero, cruza sua linha de sinal ou muda de direção. As saídas espelham a condição selecionada.

## Detalhes

- **Critérios de entrada**: Baseado em `BuyCondition` (cruzamento do zero, cruzamento da linha de sinal ou mudança de direção) no Delta-RSI.
- **Comprado/Vendido**: Ambos, controlado por `UseLong` e `UseShort`.
- **Critérios de saída**: Baseado em `ExitCondition` no Delta-RSI.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, EMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

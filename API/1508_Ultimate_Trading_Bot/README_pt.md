# Ultimate Trading Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente comprado que combina cruzamentos de RSI, média móvel, MACD e Estocástico para determinar entradas e saídas.

## Detalhes

- **Critérios de entrada**: RSI cruzando acima da zona de sobrevenda enquanto o preço está acima da MA, MACD e Estocástico cruzam para cima.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Condições de cruzamento opostas.
- **Stops**: Sem stops explícitos.
- **Valores padrão**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: RSI, MA, MACD, Stochastic
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

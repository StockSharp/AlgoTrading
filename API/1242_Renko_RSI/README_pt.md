# Estratégia Renko RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera tijolos Renko usando sinais de sobrecompra/sobrevenda do RSI.

Os testes mostram desempenho moderado e funciona melhor em mercados com tendências Renko claras.

O Renko RSI usa tijolos Renko construídos a partir do ATR e aplica um RSI curto. Um cruzamento acima do nível de sobrevenda dispara uma compra, enquanto uma queda abaixo do nível de sobrecompra dispara uma venda.

## Detalhes

- **Critérios de entrada**: RSI cruza os níveis de sobrevenda ou sobrecompra.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, Renko
  - Stops: Não
  - Complexidade: Básico
  - Período: Renko
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

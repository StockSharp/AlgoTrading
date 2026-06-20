# Macd Vwap Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores MACD e VWAP. Entra comprado quando MACD > Signal e preço > VWAP. Entra vendido quando MACD < Signal e preço < VWAP.

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado de criptomoedas.

O momentum do MACD é medido em relação à linha VWAP. Operações compradas buscam força do MACD abaixo do VWAP, enquanto as vendidas se formam acima dele.

Ideal para operadores de momentum intradiário que usam referências ponderadas por volume. Stops baseados em ATR gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD > Signal && Close > VWAP`
  - Vendido: `MACD < Signal && Close < VWAP`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento do MACD na direção oposta
- **Stops**: Percentual usando `StopLossPercent`
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MACD, VWAP
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


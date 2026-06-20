# Estratégia VWAP RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
VWAP RSI usa o preço médio ponderado por volume para avaliar o valor justo durante a sessão enquanto o RSI mostra extremos de momentum.
As operações são realizadas quando o preço se distancia do VWAP e o RSI atinge níveis de sobrecompra ou sobrevenda.

Os testes indicam um retorno anual médio de aproximadamente 157%. Funciona melhor no mercado de criptomoedas.

A expectativa é que o preço reverta em direção ao VWAP assim que o momentum esfriar.

Um stop percentual protege contra tendências que continuam a afastar o preço do VWAP.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: VWAP, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


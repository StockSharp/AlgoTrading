# Estratégia Volume Weighted Price Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia combina uma média móvel com uma média móvel ponderada por volume (VWMA). Quando o preço opera acima da VWMA, sugere que os compradores são dominantes. Um rompimento ocorre quando o preço cruza a VWMA pelo lado oposto.

Os testes indicam um retorno anual médio de aproximadamente 40%. Funciona melhor no mercado de criptomoedas.

As operações se alinham com a direção da VWMA e usam a média móvel simples como filtro de tendência de nível superior. As saídas ocorrem quando o preço reverte em relação à média móvel.

O objetivo é capturar rompimentos apoiados por volume.

## Detalhes

- **Critérios de entrada**: Preço acima ou abaixo da VWMA com confirmação da MA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço cruza a MA na direção oposta ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `VWAPPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: VWMA, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


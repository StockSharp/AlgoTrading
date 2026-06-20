# RSI Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no RSI Laguerre

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado de criptomoedas.

O RSI Laguerre suaviza o RSI padrão para reduzir o ruído. A estratégia compra quando o valor Laguerre cruza para cima a partir da zona de sobrevenda e vende quando cruza para baixo a partir da sobrecompra, saindo quando retorna aos níveis médios.

A filtragem Laguerre ajuda a evitar condições agitadas que afetam os sinais do RSI regular. O método é popular para capturar oscilações em gráficos intradiários enquanto ignora flutuações menores.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `Gamma` = 0.7m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


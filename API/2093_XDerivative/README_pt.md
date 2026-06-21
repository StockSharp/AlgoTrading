# Estratégia XDerivative
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia XDerivative rastreia mudanças no momentum do preço usando uma taxa de variação suavizada. O expert MQL original combina um cálculo de taxa de variação com suavização Jurik para detectar pontos de inflexão. A versão do StockSharp reutiliza indicadores integrados para implementar o mesmo conceito.

A estratégia calcula a taxa de variação ao longo de `RocPeriod` barras e a suaviza com uma Jurik Moving Average de comprimento `MaLength`. Quando a derivada suavizada forma um vale (o valor anterior é menor que seu antecessor e o valor atual sobe acima do anterior), a estratégia entra ou muda para uma posição comprada. Quando um pico se forma (o valor anterior é maior que seu antecessor e o valor atual cai abaixo dele), a estratégia entra ou muda para uma posição vendida. Stops de proteção gerenciam as saídas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Derivada suavizada vira para cima após um mínimo local.
  - Vendido: Derivada suavizada vira para baixo após um máximo local.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Virada oposta da derivada ou stop de proteção.
- **Stops**: Sim, take profit e stop loss em percentual.
- **Valores padrão**:
  - `RocPeriod` = 34
  - `MaLength` = 7
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RateOfChange, JurikMovingAverage
  - Stops: Sim
  - Complexidade: Básico
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

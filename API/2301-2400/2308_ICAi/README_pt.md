# Estratégia ICAi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador de média móvel adaptativa ICAi. O indicador suaviza o preço e adapta sua inclinação usando o desvio padrão. Posições compradas são abertas quando o indicador vira para cima; posições vendidas, quando vira para baixo.

O algoritmo funciona em qualquer mercado onde dados de candles estejam disponíveis. As configurações padrão usam um período de 4 horas e um comprimento de suavização de 12.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Prev < PrevPrev && Current >= Prev`
  - Vendido: `Prev > PrevPrev && Current <= Prev`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Stop loss e take profit fixos opcionais
- **Valores padrão**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ICAi
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia de Três Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Três Parabolic SAR usa três indicadores Parabolic SAR calculados em velas de 6 horas, 3 horas e 1 hora. Uma negociação é aberta no período de 1 hora quando os dois períodos superiores confirmam a direção e o SAR de 1 hora muda de posição.

## Detalhes

- **Critérios de entrada**:
  - O SAR nas velas de 6h está abaixo do fechamento e o de 3h também para comprado; acima para vendido.
  - Nas velas de 1h o SAR cruza o preço: de cima para baixo para comprado, de baixo para cima para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: A posição é fechada quando o SAR de 1h se move contra a posição ou quando qualquer SAR de período superior se reverte.
- **Stops**: Não.
- **Valores padrão**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Não
  - Complexidade: Básico
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

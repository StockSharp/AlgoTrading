# Color JLaguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no oscilador Laguerre codificado por cores.

O indicador suaviza o movimento de preços com um filtro Jurik e pinta sua linha de acordo com a posição dentro de níveis predefinidos. Uma mudança de cor marca uma potencial mudança de tendência.

A estratégia entra comprado quando o oscilador cruza o nível médio para cima e vendido quando cruza para baixo. As posições são fechadas quando o oscilador atinge níveis extremos ou um sinal oposto aparece.

## Detalhes

- **Critérios de entrada**: Mudança de cor do oscilador Laguerre em torno do nível médio.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou atingimento de nível extremo.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiLength` = 14
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Por hora (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

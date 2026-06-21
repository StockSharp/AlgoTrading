# Estratégia de Rompimento de Triângulo com TP, SL e Filtro EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta padrões de triângulo a partir de máximos e mínimos de pivô. Entra comprado no rompimento acima do triângulo, opcionalmente exigindo que o preço esteja acima de EMA20 e EMA50, e utiliza take-profit e stop-loss baseados em percentual.

## Detalhes

- **Critérios de entrada**: fechamento acima da linha superior do triângulo com filtro opcional EMA20/EMA50
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: take-profit ou stop-loss em percentual
- **Stops**: Sim
- **Valores padrão**:
  - `PivotLength` = 5
  - `TakeProfitPercent` = 3
  - `StopLossPercent` = 1.5
  - `UseEmaFilter` = true
  - `EmaFast` = 20
  - `EmaSlow` = 50
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: EMA, Pivot
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

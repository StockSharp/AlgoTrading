# Estratégia Universum 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no oscilador DeMarker que abre posições a cada barra concluída e ajusta o volume usando um esquema martingale.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `DeMarker > 0.5`
  - Vendido: `DeMarker < 0.5`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - As posições são fechadas por take profit ou stop loss
- **Stops**: Pontos absolutos via `TakeProfitPoints` e `StopLossPoints`
- **Valores padrão**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: DeMarker
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

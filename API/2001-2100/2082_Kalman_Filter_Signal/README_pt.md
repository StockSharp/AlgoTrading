# Estratégia de Sinal do Filtro Kalman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o indicador Kalman Filter para detectar mudanças de direção. A saída do filtro é comparada com o preço ou sua inclinação dependendo do modo de sinal selecionado. Quando o sinal se torna altista, a estratégia abre uma posição comprada; quando baixista, abre uma vendida. As posições são revertidas em sinais opostos. Stop loss e take profit são aplicados usando distâncias absolutas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: sinal muda para altista
  - Vendido: sinal muda para baixista
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Stop loss e take profit absolutos
- **Valores padrão**:
  - `ProcessNoise` = 1.0
  - `MeasurementNoise` = 1.0
  - `CandleType` = TimeSpan.FromHours(3).TimeFrame()
  - `Mode` = SignalModes.Kalman
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Kalman Filter
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

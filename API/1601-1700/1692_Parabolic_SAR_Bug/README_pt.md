# Estratégia Parabolic SAR Bug
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Parabolic SAR Bug** opera reversões de tendência usando o indicador Parabolic SAR. Quando o SAR vira abaixo do preço, a estratégia entra comprada, e quando o SAR vira acima do preço entra vendida. O modo reverse opcional inverte os sinais. Stop loss protetor, take profit e trailing stop são suportados através do módulo de proteção de posição integrado.

## Detalhes

- **Critérios de entrada**: Mudança de direção do Parabolic SAR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal SAR oposto ou stop protetor.
- **Stops**: Stop loss, take profit, trailing stop opcional.
- **Valores padrão**:
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Stop loss, take profit
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

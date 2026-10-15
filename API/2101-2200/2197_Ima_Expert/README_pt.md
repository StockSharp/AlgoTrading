# Estratégia Ima Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base na velocidade relativa do preço em relação à sua média móvel.
O rácio `Close / SMA - 1` é comparado entre duas velas consecutivas. Um forte aumento abre uma posição comprada, enquanto uma forte queda abre uma posição vendida.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - Vendido: `(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **Critérios de saída**: Sinal oposto
- **Dimensionamento da posição**: `RiskLevel` e `StopLossTicks` definem o volume da operação, limitado por `MaxVolume`
- **Comprado/Vendido**: Ambos
- **Stops**: Nenhum
- **Valores padrão**:
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

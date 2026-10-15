# Estratégia BTFD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de compra em quedas baseada em volume e RSI, com cinco níveis de take-profit e um stop de proteção.

## Detalhes

- **Critérios de entrada**: Pico de volume acima da SMA e RSI abaixo da zona de sobrevenda.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Cinco alvos de take-profit escalonados ou stop loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: RSI, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (3m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia MA SAR ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Média Móvel, Parabolic SAR e Índice Direcional Médio (ADX).
Compra quando o preço está acima tanto da média móvel quanto do SAR e o +DI está acima do -DI.
Vende quando o preço está abaixo tanto da média móvel quanto do SAR e o +DI está abaixo do -DI.
As posições são fechadas quando o preço cruza o SAR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > MA && +DI >= -DI && Close > SAR`
  - Vendido: `Close < MA && +DI <= -DI && Close < SAR`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O preço cruza o Parabolic SAR
- **Stops**: Não
- **Valores padrão**:
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, Parabolic SAR, ADX
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

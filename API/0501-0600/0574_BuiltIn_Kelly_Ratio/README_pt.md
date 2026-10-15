# Razão Kelly Integrada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento de canal usando uma média móvel e bandas ATR com dimensionamento de posição baseado na razão Kelly.

## Detalhes

- **Critérios de entrada**: Preço cruzando acima ou abaixo das bandas baseadas em ATR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take-profit e stop-loss opcionais.
- **Stops**: Opcional.
- **Valores padrão**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MA, ATR
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

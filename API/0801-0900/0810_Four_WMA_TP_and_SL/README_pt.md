# Estratégia de Quatro WMA com TP e SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o cruzamento de quatro médias móveis com take profit, stop loss e condição de saída alternativa opcionais.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Long MA1 cruza acima de Long MA2
  - Vendido: Short MA1 cruza abaixo de Short MA2
- **Comprado/Vendido**: Configurável
- **Stops**: TP e SL baseados em percentual
- **Valores padrão**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Médias móveis
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

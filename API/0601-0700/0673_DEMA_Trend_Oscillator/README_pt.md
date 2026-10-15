# Estratégia de Oscilador de Tendência DEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia normaliza a Média Móvel Exponencial Dupla (DEMA) com uma média móvel e desvio padrão. Entra comprado quando o valor normalizado supera o limiar longo e o preço permanece acima da banda superior; entra vendido quando está abaixo do limiar curto e o preço está abaixo da banda inferior. Utiliza stop trailing baseado em ATR, stop-loss de banda e take profit de risco-recompensa.

## Detalhes

- **Critérios de entrada**:
  - Comprado: valor normalizado > `LongThreshold` e mínimo > banda superior
  - Vendido: valor normalizado < `ShortThreshold` e máximo < banda inferior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: o preço atinge o take profit, stop-loss de banda ou trailing stop
  - Vendido: o preço atinge o take profit, stop-loss de banda ou trailing stop
- **Stops**: Stop-loss de banda, trailing ATR, take profit de risco-recompensa
- **Valores padrão**:
  - `DemaPeriod` = 40
  - `BaseLength` = 20
  - `LongThreshold` = 55m
  - `ShortThreshold` = 45m
  - `RiskReward` = 1.5m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: DEMA, SMA, StandardDeviation, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

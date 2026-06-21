# Estratégia de Bollinger Bands Modificada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera rompimentos de Bollinger Bands com um filtro de tendência EMA opcional. Entra comprado quando o preço cruza acima da banda superior e vendido quando cruza abaixo da banda inferior.

O stop loss é colocado no máximo ou mínimo recente e o take profit é um múltiplo do risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço cruza acima da banda superior de Bollinger
  - Vendido: preço cruza abaixo da banda inferior de Bollinger
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: stop no mínimo recente, alvo em risco * fator
  - Vendido: stop no máximo recente, alvo em risco * fator
- **Stops**: Máximo/mínimo das últimas N velas
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, EMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

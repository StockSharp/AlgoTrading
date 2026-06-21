# Estratégia de Stochastic NR em Color
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando um oscilador Stochastic com vários modos selecionáveis. Cada modo define como as linhas %K e %D são interpretadas para gerar sinais de compra e venda.

Modos:

- **Breakdown** – comprado quando %K cruza acima do nível 50, vendido quando cai abaixo.
- **OscTwist** – reage a mudanças de direção de %K.
- **SignalTwist** – reage a mudanças de direção de %D.
- **OscDisposition** – comprado quando %K cruza acima de %D, vendido quando cruza abaixo.
- **SignalBreakdown** – opera quando %D cruza o nível 50.

Sinais opostos fecham posições existentes e abrem novas na direção contrária. O controle de risco é gerenciado por níveis fixos de stop-loss e take-profit em percentual.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Depende do modo selecionado, ver acima.
  - **Vendido**: Depende do modo selecionado, ver acima.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou proteção de stop.
- **Stops**: Sim, `StopLossPercent` e `TakeProfitPercent`.
- **Valores padrão**:
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 hour
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

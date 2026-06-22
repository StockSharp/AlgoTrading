# Estratégia de Abertura e Fechamento no Tempo v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia baseada em tempo que abre operações em um horário específico e as fecha mais tarde no dia. A direção da operação é confirmada comparando uma média móvel exponencial rápida e uma lenta. Os níveis de stop-loss e take-profit são expressos em ticks.

## Detalhes

- **Critérios de Entrada**: No `OpenTime`, ir comprado se a EMA rápida estiver acima da EMA lenta, ir vendido se estiver abaixo. A direção depende de `TradeMode`.
- **Comprado/Vendido**: Configurável (comprar, vender ou ambos).
- **Critérios de Saída**: As posições são fechadas no `CloseTime` ou por stops de proteção.
- **Stops**: Sim, tanto stop-loss como take-profit em ticks.
- **Valores padrão**:
  - `OpenTime` = 05:00
  - `CloseTime` = 21:01
  - `SlowPeriod` = 200
  - `FastPeriod` = 50
  - `StopLossTicks` = 30
  - `TakeProfitTicks` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Baseado em tempo
  - Direção: Configurável
  - Indicadores: EMA
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

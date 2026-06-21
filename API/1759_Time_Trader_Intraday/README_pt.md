# Estratégia de Operador Intradiário por Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre posições compradas e/ou vendidas em um horário específico do dia com distâncias predefinidas de stop loss e take profit. É útil para testar entradas baseadas em tempo sem nenhuma confirmação de indicadores.

## Detalhes

- **Critérios de entrada**: Gatilho baseado em tempo na hora e minuto configurados.
- **Comprado/Vendido**: Ambas as direções (configurável).
- **Critérios de saída**: Stop protetor ou alvo.
- **Stops**: Sim.
- **Valores padrão**:
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

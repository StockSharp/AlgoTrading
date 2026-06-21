# Estratégia LANZ 5.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia LANZ 5.0 opera na direção de uma EMA de 200 períodos e requer três velas consecutivas da mesma cor. Limita as operações por contagem diária, janela de horário de Nova York e distância mínima entre entradas.

## Detalhes

- **Critérios de entrada**:
  - Preço acima da EMA e três velas de alta para entradas compradas.
  - Preço abaixo da EMA e três velas de baixa para entradas vendidas (opcional).
- **Comprado/Vendido**: Comprado por padrão.
- **Critérios de saída**:
  - Stop-loss ou take-profit fixos.
  - Fechamento manual no horário configurado.
- **Stops**:
  - Stop loss = 40 pips.
  - Take profit = 120 pips.
- **Valores padrão**:
  - `EmaPeriod` = 200
  - `MaxTrades` = 99
  - `MinDistancePips` = 25
  - `StopLossPips` = 40
  - `TakeProfitPips` = 120
  - `StartHour` = 19
  - `EndHour` = 15
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

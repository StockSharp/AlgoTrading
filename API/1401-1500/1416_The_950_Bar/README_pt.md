# Estratégia da Vela das 9:50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera a vela de cinco minutos das 9:50 AM de Nova York. Após o fechamento da vela, entra na direção dela com alvo de lucro fixo e stop definidos em ticks.

## Detalhes
- **Critérios de entrada**: Direção da vela de cinco minutos das 9:50 AM NY.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Atingir o alvo ou o stop.
- **Stops**: Stop e alvo fixos.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TickSize` = 0.25
  - `TargetTicks` = 150
  - `StopTicks` = 200
- **Filtros**:
  - Categoria: Tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

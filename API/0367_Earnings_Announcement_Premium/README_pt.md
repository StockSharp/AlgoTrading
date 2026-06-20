# Prêmio por Anúncio de Resultados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de **Prêmio por Anúncio de Resultados** compra ações alguns dias antes dos anúncios de resultados e sai logo após a divulgação.

## Detalhes
- **Critérios de entrada**: Comprar `DaysBefore` dias antes dos resultados.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Vender `DaysAfter` dias após os resultados.
- **Stops**: Não.
- **Valores padrão**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Event-driven
  - Direção: Comprado
  - Indicadores: Calendar
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

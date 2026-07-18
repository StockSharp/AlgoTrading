# Anúncios de Resultados com Recompras de Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de **Anúncios de Resultados com Recompras de Ações** compra ações com programas de recompra ativos alguns dias antes dos anúncios de resultados e sai logo após o relatório.

## Detalhes
- **Critérios de entrada**: Comprar `DaysBefore` dias antes dos resultados se a empresa tiver uma recompra ativa.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Vender `DaysAfter` dias após a data dos resultados.
- **Stops**: Não.
- **Valores padrão**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Event-driven
  - Direção: Comprado
  - Indicadores: Buyback + Calendar
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

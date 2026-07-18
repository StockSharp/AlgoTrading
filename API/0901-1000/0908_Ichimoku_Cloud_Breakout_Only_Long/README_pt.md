# Rompimento da Nuvem Ichimoku Somente Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre posições compradas quando o preço rompe acima da nuvem Ichimoku e encerra quando o preço cai abaixo dela. Apenas operações de compra são realizadas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close` cruza acima de `max(SenkouA, SenkouB)`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - `Close` cruza abaixo de `min(SenkouA, SenkouB)`
- **Stops**: Nenhum
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Ichimoku
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

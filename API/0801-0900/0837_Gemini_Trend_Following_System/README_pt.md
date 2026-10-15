# Sistema de Seguidor de Tendência Gemini
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência que compra retrações para a SMA de 50 dias dentro de uma forte tendência de alta confirmada pela SMA de 200 dias e pelo filtro anual de Rate of Change.

## Detalhes

- **Critérios de entrada**: O preço se recupera acima da SMA 50 após uma retração recente em uma tendência de alta confirmada.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Cruzamento de morte da SMA 50 abaixo da SMA 200 ou stop catastrófico.
- **Stops**: Stop catastrófico opcional.
- **Valores padrão**:
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: SMA, RateOfChange, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

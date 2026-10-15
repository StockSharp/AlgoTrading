# Estratégia de Compra na Nuvem Ichimoku com Saída EMA Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia de compra na Nuvem Ichimoku com saída EMA personalizada e filtro de volume. A estratégia compra quando o preço está acima da nuvem e o volume supera sua média. Opcionalmente requer que o preço permaneça acima da EMA. A posição é encerrada quando o preço cai abaixo da EMA ou quando o stop-loss é acionado.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Price > Cloud && Volume > AvgVolume && (Price > EMA if enabled)`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - `Price < EMA`
- **Stops**: Baseado em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `EmaLength` = 44
  - `VolumeAvgPeriod` = 10
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Nuvem Ichimoku, EMA, Volume
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

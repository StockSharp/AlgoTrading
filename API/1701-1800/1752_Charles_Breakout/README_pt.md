# Estratégia de Rompimento Charles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento baseada nos níveis máximos e mínimos diários. Ela busca que o preço se mova além do intervalo do dia anterior com um filtro de tendência RSI e EMA. A estratégia calcula o máximo e mínimo diários, desloca-os por um delta configurável e entra comprado acima do nível superior ou vendido abaixo do nível inferior quando as condições de tendência são confirmadas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > DailyHigh + Delta` e `RSI > 55` e `FastEMA > SlowEMA`
  - Vendido: `Close < DailyLow - Delta` e `RSI < 45` e `FastEMA < SlowEMA`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal contrário ou proteção
- **Stops**: Take profit e stop loss configuráveis em porcentagem
- **Valores padrão**:
  - `Delta` = 0.0002m
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 1m
  - `StopLoss` = 0.5m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

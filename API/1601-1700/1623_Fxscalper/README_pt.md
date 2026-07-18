# Estratégia Fxscalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping de rompimento de Bandas de Bollinger traduzida do especialista MQL4 "fxscalper".
A estratégia se inscreve em dados de velas e Bandas de Bollinger. Quando o preço de fechamento rompe acima da banda superior abre uma posição comprada; quando o preço de fechamento rompe abaixo da banda inferior abre uma posição vendida. As posições são protegidas por níveis de stop loss e take profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > Upper Band`
  - Vendido: `Close < Lower Band`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto ou stops protetores
- **Stops**: Stop loss e take profit
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **Filtros**:
  - Categoria: Bollinger Bands
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

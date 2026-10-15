# Fractal ADX Nuvem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aproxima o expert MQL original `Fractal_ADX_Cloud` usando o indicador Average Directional Index no StockSharp. Funciona com velas de quatro horas e analisa o cruzamento dos componentes +DI e -DI. Quando o componente de alta (+DI) sobe acima do de baixa (-DI), a estratégia fecha qualquer posição vendida e pode abrir uma nova comprada. Se -DI sobe acima de +DI, a lógica é espelhada para trades vendidos.

As proteções de stop-loss e take-profit são aplicadas em unidades de preço absolutas. Parâmetros adicionais permitem habilitar ou desabilitar a abertura e o fechamento de posições em cada direção.

## Detalhes

- **Critérios de entrada**: Cruzamento das linhas +DI e -DI do ADX.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim, usando distâncias de preço absolutas.
- **Valores padrão**:
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX
  - Stops: Sim
  - Complexidade: Básico
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

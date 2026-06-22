# Estratégia I4 DRF v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia I4 DRF v2 aplica o indicador personalizado i4_DRF_v2 que conta o número de fechamentos de alta e baixa em uma janela deslizante.
Dependendo do parâmetro TrendModes, pode funcionar no modo contrário (Direct) ou de seguimento de tendência (NotDirect).
A estratégia abre e fecha posições quando o indicador muda de sinal e suporta stop loss e take profit opcionais em passos de preço.

## Detalhes

- **Critérios de entrada**: Inversão de sinal do indicador de acordo com TrendModes
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto ou stop loss/take profit
- **Stops**: Sim
- **Valores padrão**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendModes` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Personalizado
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

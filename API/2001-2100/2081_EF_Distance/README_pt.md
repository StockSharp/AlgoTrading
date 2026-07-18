# Estratégia de Reversão EF Distance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação StockSharp do consultor especialista MetaTrader "Exp_EF_distance". Substitui os indicadores originais EF Distance e Flat-Trend por uma média móvel simples (SMA) e um filtro Average True Range (ATR) para detectar pontos de virada do mercado. O algoritmo observa três valores consecutivos de SMA e identifica mínimos ou máximos locais. Uma posição comprada é aberta quando a SMA forma um fundo local e a volatilidade supera o limite. Uma posição vendida é aberta no padrão oposto. As posições são fechadas em sinais opostos ou quando os níveis de stop-loss ou take-profit são atingidos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `SMA(t-1) < SMA(t-2)` e `SMA(t) > SMA(t-1)` e `ATR(t) ≥ AtrThreshold`.
  - **Vendido**: `SMA(t-1) > SMA(t-2)` e `SMA(t) < SMA(t-1)` e `ATR(t) ≥ AtrThreshold`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal reverso na direção oposta.
  - Stop-loss ou take-profit atingido.
- **Indicadores**:
  - Média Móvel Simples (SMA) – aproximação do EF Distance.
  - Average True Range (ATR) – filtro de volatilidade.
- **Valores padrão**:
  - `SMA period` = 10.
  - `ATR period` = 20.
  - `ATR threshold` = 1.
  - `StopLoss` = 100.
  - `TakeProfit` = 200.
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Dois
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Configurável
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (usa pontos de virada)
  - Nível de risco: Médio

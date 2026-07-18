# Previsão de Intervalo Monte Carlo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Previsão de Intervalo Monte Carlo utiliza simulações Monte Carlo com volatilidade baseada em ATR para projetar o intervalo futuro de preços. A estratégia entra comprada quando o preço simulado médio supera o preço atual e entra vendida quando cai abaixo.

## Detalhes
- **Dados**: Velas de preço com ATR.
- **Critérios de entrada**:
  - **Comprado**: O preço esperado das simulações está acima do preço atual.
  - **Vendido**: O preço esperado das simulações está abaixo do preço atual.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `ForecastPeriod` = 20
  - `Simulations` = 100
- **Filtros**:
  - Categoria: Estatística
  - Direção: Comprado & Vendido
  - Indicadores: ATR
  - Complexidade: Moderado
  - Nível de risco: Médio

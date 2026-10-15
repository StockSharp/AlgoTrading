# Estratégia de Cruzamento de PVT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento do indicador Price Volume Trend (PVT) e sua média móvel exponencial (EMA). Uma posição comprada é aberta quando o PVT cruza acima de sua EMA, e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: PVT cruza acima de sua EMA.
  - **Vendido**: PVT cruza abaixo de sua EMA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Reverter posição ao sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `EmaLength` = 20.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: PVT, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário

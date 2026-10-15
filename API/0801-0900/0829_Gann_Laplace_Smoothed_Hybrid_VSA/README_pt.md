# Gann Laplace Híbrido VSA Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um filtro de tendência no estilo Gann com análise de spread de volume (VSA) suavizada com Laplace. O valor VSA é calculado como o spread de preço dividido pelo intervalo da vela e multiplicado pelo volume, depois suavizado com uma EMA. Operações são abertas quando o VSA suavizado se alinha com o preço em relação à média móvel de tendência.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: VSA suavizado > 0 e fechamento > MA de tendência.
  - **Vendido**: VSA suavizado < 0 e fechamento < MA de tendência.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: o VSA suavizado se torna negativo.
  - **Vendido**: o VSA suavizado se torna positivo.
- **Stops**: Usa StartProtection.
- **Valores padrão**:
  - `Trend Period` = 20
  - `VSA Smoothing` = 14
  - `Candle Type` = 15m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MA, Volume
  - Stops: Sim
  - Complexidade: Médio
  - Período: Médio prazo

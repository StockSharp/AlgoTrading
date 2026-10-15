# Estratégia Color BB Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza as Bandas de Bollinger para classificar as velas em zonas de alta, baixa ou neutras. Abre uma posição comprada quando o preço fecha acima da banda superior, abre uma posição vendida quando o preço fecha abaixo da banda inferior, e fecha qualquer posição quando o preço retorna entre as bandas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cruza acima da banda superior vindo de fora.
  - **Vendido**: O preço de fechamento cruza abaixo da banda inferior vindo de fora.
- **Critérios de saída**: O preço retorna entre as bandas superior e inferior.
- **Indicadores**: Bandas de Bollinger.
- **Valores padrão**:
  - `BollingerPeriod` = 100
  - `BollingerDeviation` = 1.0
  - `CandleType` = período de 4 horas
- **Direção**: Comprado e vendido.
- **Stops**: Nenhum.
- **Complexidade**: Moderado.
- **Período**: Médio prazo.

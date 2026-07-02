# Estratégia Ehlers SwamiCharts RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Calcula a média dos valores de RSI dos períodos 2–48 para construir um mapa de cores. Comprado quando a cor média é verde, vendido quando é vermelha.

## Detalhes

- **Critérios de entrada**: Cor média verde (`Color1Avg` == 255 e `Color2Avg` > `LongColor`) para comprado; vermelho (`Color1Avg` > `ShortColor` e `Color2Avg` == 255) para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

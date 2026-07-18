# KDJ Adaptativo (MTF)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia KDJ Adaptativo combina valores do oscilador KDJ de três períodos de tempo. Cada período é suavizado com uma EMA e combinado usando pesos ajustáveis. A força da tendência é medida com uma SMA do oscilador combinado, que adapta os níveis de sobrecompra e sobrevenda.

A estratégia entra comprada quando a linha J está abaixo do nível de compra adaptativo e a linha K cruza acima da linha D. Entra vendida quando a linha J está acima do nível de venda adaptativo e a linha K cruza abaixo da linha D.

## Detalhes

- **Critérios de entrada**: Cruzamento KDJ com J abaixo/acima de níveis dinâmicos.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic, EMA, SMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

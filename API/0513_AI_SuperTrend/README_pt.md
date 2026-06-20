# Estratégia AI SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AI SuperTrend combina o indicador SuperTrend com médias móveis ponderadas do preço e da linha SuperTrend. Uma operação comprada é aberta quando o SuperTrend vira para cima e a WMA do preço se move acima da WMA do SuperTrend. Uma operação vendida é aberta nas condições opostas. As posições são protegidas com um trailing stop dinâmico baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A direção do SuperTrend vira para cima e a WMA do preço está acima da WMA do SuperTrend.
  - **Vendido**: A direção do SuperTrend vira para baixo e a WMA do preço está abaixo da WMA do SuperTrend.
- **Critérios de saída**:
  - Reversão de tendência ou trailing stop ATR.
- **Stops**: Trailing stop ATR dinâmico.
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, WMA, ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

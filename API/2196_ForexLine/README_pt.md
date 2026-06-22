# Estratégia ForexLine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia ForexLine é um sistema seguidor de tendência derivado do indicador MetaTrader "ForexLine". Ela aplica dois estágios de médias móveis ponderadas ao preço para construir linhas rápidas e lentas. Os cruzamentos entre essas linhas com dupla suavização são usados para determinar os sinais de entrada.

A estratégia compra quando a linha rápida cruza acima da linha lenta e vende quando a linha rápida cruza abaixo da linha lenta. Cada média móvel usa um processo de suavização em dois passos que ajuda a filtrar o ruído de mercado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A WMA duplamente suavizada rápida cruza acima da WMA duplamente suavizada lenta.
  - **Vendido**: A WMA duplamente suavizada rápida cruza abaixo da WMA duplamente suavizada lenta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - O cruzamento oposto fecha a posição existente.
- **Stops**: Não incluídos; podem ser adicionados externamente.
- **Valores padrão**:
  - `FastLength1` = 5
  - `FastLength2` = 10
  - `SlowLength1` = 20
  - `SlowLength2` = 20
  - `CandleType` = período de 8 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias móveis ponderadas
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

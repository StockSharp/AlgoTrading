# Estratégia de Pacote de Filtros de Reamostagem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia amostra o preço a cada N barras e o suaviza com uma média móvel. Vai comprado quando o valor filtrado sobe e o preço negocia acima dele, e vai vendido quando o valor filtrado cai e o preço está abaixo.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: a inclinação do filtro é ascendente e o fechamento está acima do filtro.
  - **Vendido**: a inclinação do filtro é descendente e o fechamento está abaixo do filtro.
- **Critérios de saída**: sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BarsPerSample` = 5
  - `MovingAverageType` = EMA
  - `MaPeriod` = 9
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: Média móvel
  - Complexidade: Simples
  - Nível de risco: Médio

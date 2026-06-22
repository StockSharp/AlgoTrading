# Estratégia de Histograma RAVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o expert MetaTrader RAVI Histogram para o StockSharp. Ela mede a força da tendência como a diferença percentual entre uma EMA rápida e uma lenta. O resultado é comparado com níveis superior e inferior para decidir quando operar.

Quando o valor RAVI sobe acima do nível superior, o mercado é considerado em alta. As posições vendidas são fechadas e, se habilitado, uma posição comprada é aberta. Quando o valor cai abaixo do nível inferior, a estratégia fecha as compradas e pode abrir uma vendida. Por padrão opera com velas de quatro horas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RAVI cruza para cima através de `UpLevel`.
  - **Vendido**: RAVI cruza para baixo através de `DownLevel`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal RAVI oposto fecha as posições existentes.
- **Stops**: Nenhum.
- **Filtros**: Nenhum.
- **Período**: velas de 4 horas por padrão.
- **Parâmetros**:
  - `FastLength` e `SlowLength` – períodos de EMA para o cálculo RAVI.
  - `UpLevel` e `DownLevel` – limiares que definem zonas de tendência.
  - `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` – habilitam ou desabilitam operações em cada direção.

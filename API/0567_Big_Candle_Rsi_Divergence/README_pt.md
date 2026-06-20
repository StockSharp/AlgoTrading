# Divergência RSI de Vela Grande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Identifica velas inusualmente grandes em relação às cinco barras anteriores e compara os valores de RSI rápido e lento. As operações seguem a direção da vela e usam um stop trailing atrasado que se ativa somente após o preço se mover um número definido de ticks em lucro.

O stop trailing começa assim que o limite de lucro é atingido e, em seguida, rastreia o preço a uma distância fixa, enquanto um stop fixo inicial protege a operação desde o início.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O corpo da vela atual é maior do que os cinco anteriores e fecha em alta.
  - **Vendido**: O corpo da vela atual é maior do que os cinco anteriores e fecha em baixa.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop inicial ou stop trailing atingido.
- **Stops**: Sim, stop trailing atrasado.
- **Valores padrão**:
  - `TrailStartTicks` = 200
  - `TrailDistanceTicks` = 150
  - `InitialStopLossTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

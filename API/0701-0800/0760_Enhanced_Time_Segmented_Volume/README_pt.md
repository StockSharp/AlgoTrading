# Volume Segmentado por Tempo Aprimorado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Volume Segmentado por Tempo Aprimorado monitora as variações de preço ponderadas por volume. Quando o TSV está acima de sua média móvel e é positivo, a estratégia compra. Quando o TSV está abaixo da média e é negativo, vende a descoberto.

## Detalhes

- **Critérios de entrada**: TSV em relação à sua média móvel.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `TsvLength` = 13
  - `MaLength` = 7
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Volume, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

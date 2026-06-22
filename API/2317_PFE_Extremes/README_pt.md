# PFE Extremos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia rompimentos do indicador Polarized Fractal Efficiency (PFE). Quando o PFE cruza acima do nível superior, a estratégia fecha qualquer posição vendida e abre uma comprada. Quando o PFE cruza abaixo do nível inferior, fecha posições compradas e abre uma vendida.

O indicador PFE avalia quão eficientemente o preço está se movendo em relação ao seu caminho. Valores próximos a +1 sugerem forte movimento ascendente, enquanto valores próximos a -1 mostram forte movimento descendente. Cruzamentos de limiar podem destacar o início de uma nova tendência.

## Detalhes

- **Critérios de entrada**: PFE cruza acima de `UpLevel` para comprado ou abaixo de `DownLevel` para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rompimento do nível oposto ou sinal de reversão.
- **Stops**: Não usados por padrão; podem ser adicionados via proteção de posição.
- **Valores padrão**:
  - `PfePeriod` = 5
  - `UpLevel` = 0.5
  - `DownLevel` = -0.5
  - `CandleType` = período de 4 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: PFE
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

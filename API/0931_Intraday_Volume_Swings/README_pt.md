# Estratégia Intradiária de Oscilações por Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera quando o preço entra em regiões de oscilação definidas por volume do dia atual ou do dia anterior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço penetra na região de oscilação alta.
  - **Vendido**: O preço penetra na região de oscilação baixa.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `RegionMustClose` = true
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Não
  - Complexidade: Médio
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

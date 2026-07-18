# Não Me Faça Cruzar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA com deslocamento vertical.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA curta deslocada cruza acima da EMA longa deslocada.
  - **Vendido**: EMA curta deslocada cruza abaixo da EMA longa deslocada.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ShortEmaLength` = 9
  - `LongEmaLength` = 21
  - `ShiftAmount` = -50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

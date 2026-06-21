# Filtro de Redução de Atraso de Séries Temporais por Cryptorhythms
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no filtro de redução de atraso da EMA.

O algoritmo compara o preço com uma EMA ajustada ao atraso e opera nos cruzamentos.

## Detalhes

- **Critérios de entrada**: Preço cruzando a EMA com atraso reduzido.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `LagReduction` = 20m
  - `EmaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

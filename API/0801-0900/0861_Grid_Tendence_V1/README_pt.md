# Estratégia Grid Tendence V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de trading em grade que reabre ou inverte posições com base em degraus de percentual de lucro.

Começa comprado e quando o lucro atinge o percentual especificado fecha e reabre na mesma direção. Quando a perda atinge o percentual, fecha e abre na direção oposta.

## Detalhes

- **Critérios de entrada**: Sempre no mercado, começando comprado. Reabre ou inverte quando lucro ou perda atinge `Percent`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Limite de lucro ou perda.
- **Stops**: Não.
- **Valores padrão**:
  - `Percent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

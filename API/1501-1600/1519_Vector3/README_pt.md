# Estratégia Vector3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera com base no alinhamento de três médias móveis.
Fica comprado quando fast > middle > slow e vendido quando fast < middle < slow.

## Detalhes

- **Critérios de entrada**: fast MA acima de middle e middle acima de slow (comprado); inverso para vendido
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

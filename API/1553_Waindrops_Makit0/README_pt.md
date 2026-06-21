# Waindrops Makit0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simplificada que compara o VWAP de duas metades de um período personalizado.

## Detalhes

- **Critérios de entrada**: VWAP da metade direita versus VWAP da metade esquerda.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: VWAP
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

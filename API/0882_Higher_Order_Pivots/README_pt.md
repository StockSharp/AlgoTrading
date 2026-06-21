# Estratégia de Pivôs de Ordem Superior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta máximos e mínimos de pivô de primeira, segunda e terceira ordem usando definições de pivô de 3 ou 5 barras. A estratégia é analítica e não coloca ordens.

## Detalhes

- **Critérios de entrada**:
  - Nenhum (apenas análise).
- **Critérios de saída**:
  - Nenhum.
- **Indicadores**:
  - Detector de pivô de 3 ou 5 barras.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **Filtros**:
  - Período único
  - Indicadores: detector de pivô
  - Stops: nenhum
  - Complexidade: Baixo

# Estratégia VoVix DEVMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia analisa o comportamento da volatilidade usando Médias Móveis de Desvio (DEVMA) construídas sobre o desvio padrão do ATR. Opera transições entre regimes de contração e expansão e utiliza saídas baseadas em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O DEVMA rápido cruza acima do DEVMA lento.
  - **Vendido**: O DEVMA rápido cruza abaixo do DEVMA lento.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop-loss e take-profit baseados em ATR.
- **Stops**: Sim, múltiplos de ATR.
- **Valores padrão**:
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

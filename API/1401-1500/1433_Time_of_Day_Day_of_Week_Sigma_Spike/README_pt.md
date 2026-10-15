# Estratégia de Pico Sigma por Hora do Dia / Dia da Semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa o z-score de retorno para destacar grandes movimentos por hora com filtros opcionais por dia.
Compra em picos e sai quando a volatilidade se normaliza.

## Detalhes

- **Critérios de entrada**: z-score absoluto >= `Threshold`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: z-score cai abaixo de `Threshold`
- **Stops**: Não
- **Valores padrão**:
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Comprado
  - Indicadores: StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia J-Lines Ribbon Motor de 4 Ciclos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia J-Lines Ribbon 4-Cycle Engine classifica o mercado em ciclos CHOP, LONG e SHORT usando uma faixa de EMAs e o Average Directional Index. As entradas ocorrem em novas detecções de ciclo e rebotes a partir de EMAs-chave, enquanto as saídas são acionadas em cruzamentos opostos ou quebras de swing.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Novo ciclo LONG ou rebote acima de EMA72/EMA126 enquanto EMA72 está acima de EMA89.
  - **Vendido**: Novo ciclo SHORT ou rebote abaixo de EMA72/EMA126 enquanto EMA72 está abaixo de EMA89.
- **Stops**: Último máximo/mínimo de swing.
- **Valores padrão**:
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ADX
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

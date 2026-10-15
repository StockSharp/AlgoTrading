# Estratégia EMA 5-8-13 com Filtro ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera cruzamentos de EMA nos períodos 5 e 8, usando uma EMA de 13 períodos para saídas. Um filtro ADX opcional confirma a força da tendência. Posições compradas ocorrem quando EMA5 cruza acima de EMA8 e ADX supera o limiar. Posições vendidas são iniciadas no sinal oposto.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA5 cruza acima de EMA8 e ADX > limiar.
  - **Vendido**: EMA5 cruza abaixo de EMA8 e ADX > limiar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: fechamento < EMA13
  - **Vendido**: fechamento > EMA13
- **Stops**: Não.
- **Valores padrão**:
  - `ADX threshold` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Simples
  - Período: Curto prazo

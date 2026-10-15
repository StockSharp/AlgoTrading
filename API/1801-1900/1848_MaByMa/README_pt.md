# Estratégia de Cruzamento MA por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um cruzamento de média móvel com duplo suavizamento.
A série de preços é suavizada por uma média móvel exponencial (EMA) rápida.
O resultado da EMA rápida é então suavizado novamente por uma EMA mais lenta.
As duas séries são comparadas para gerar sinais:
- Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta.
- Uma posição vendida é aberta quando a EMA rápida cruza abaixo da EMA lenta.
Qualquer posição oposta existente é fechada no cruzamento.

A estratégia funciona em qualquer período de candles.

## Parâmetros
- `FastLength` – período da EMA rápida.
- `SlowLength` – período da EMA lenta aplicada à saída da EMA rápida.
- `EnableLong` – permitir abertura de posições compradas.
- `EnableShort` – permitir abertura de posições vendidas.
- `CandleType` – tipo de candles usados para os cálculos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta.
- **Comprado/Vendido**: Ambas as direções suportadas.
- **Critérios de saída**:
  - Cruzamento oposto fecha uma posição existente.
- **Stops**: Nenhum stop-loss ou take-profit explícito é utilizado.
- **Valores padrão**:
  - `FastLength` = 7
  - `SlowLength` = 7
  - `EnableLong` = true
  - `EnableShort` = true
  - `CandleType` = período de 12 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Moving averages
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer

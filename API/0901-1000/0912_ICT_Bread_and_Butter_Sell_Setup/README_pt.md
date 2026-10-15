# Estratégia ICT Bread and Butter Sell-Setup
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rastreia as máximas e mínimas das sessões de Londres, Nova York e Ásia, e negocia configurações predefinidas em torno delas.

## Detalhes

- **Critérios de entrada**:
  - **NY Vendido**: o preço faz uma máxima superior à sessão de Londres e a vela fecha baixista durante a sessão de NY.
  - **London Close Compra**: entre 10:30 e 13:00 se o preço fecha abaixo da mínima da sessão de Londres.
  - **Asia Vendido**: durante a sessão asiática se o preço fecha acima da máxima da sessão asiática.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Cada operação usa stop-loss e take profit definidos em ticks.
- **Stops**: Sim.
- **Valores padrão**:
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: Price action
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

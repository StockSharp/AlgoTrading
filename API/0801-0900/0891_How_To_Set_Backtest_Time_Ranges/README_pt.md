# Como Definir Intervalos de Tempo para Backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia demonstra como restringir o trading a janelas específicas de data e tempo intradiário. Entra comprado quando uma SMA rápida cruza acima de uma SMA lenta e sai no cruzamento oposto.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A SMA rápida cruza acima da SMA lenta dentro dos intervalos de data e tempo de entrada selecionados.
- **Critérios de saída**: A SMA rápida cruza abaixo da SMA lenta dentro dos intervalos de data e tempo de saída selecionados.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `FromDate` = 2021-01-01
  - `ThruDate` = 2112-01-01
  - `EntryStart` = 00:00
  - `EntryEnd` = 00:00
  - `ExitStart` = 00:00
  - `ExitEnd` = 00:00
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: SMA
  - Complexidade: Baixo
  - Nível de risco: Médio

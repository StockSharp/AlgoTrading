# Estratégia de Semáforo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma abordagem de seguidor de tendência que usa um conjunto de médias móveis coloridas como um semáforo para determinar a direção de trading.
A estratégia aguarda que o preço esteja dentro de uma zona predefinida e então verifica a ordem das médias antes de entrar no mercado.

## Detalhes

- **Zona de entrada**:
  - Padrão: o preço deve estar entre as SMA vermelha (lenta) e amarela (média).
  - Se `UseBlueRange` estiver ativado: o preço deve estar entre as linhas alta e baixa do canal EMA azul.
- **Critérios de entrada**:
  - Comprado: `green EMA > blueHigh EMA > yellow SMA > red SMA` e `price > green EMA`.
  - Vendido: `green EMA < blueLow EMA < yellow SMA < red SMA` e `price < green EMA`.
- **Critérios de saída**:
  - Opcional: se `CloseOnCross` estiver ativado, a posição fecha quando a EMA verde cruza a SMA amarela na direção oposta.
- **Stops**: Take profit e stop loss opcionais medidos em passos de preço.
- **Comprado/Vendido**: Ambos.
- **Valores padrão**:
  - `RedMaPeriod` = 120
  - `YellowMaPeriod` = 55
  - `GreenMaPeriod` = 5
  - `BlueMaPeriod` = 24
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TakeProfitTicks` = 120
  - `StopLossTicks` = 60
  - `UseBlueRange` = false
  - `CloseOnCross` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias móveis
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado

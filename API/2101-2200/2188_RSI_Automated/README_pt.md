# Estratégia RSI Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de momentum que utiliza o Índice de Força Relativa (RSI) para operar em condições extremas de sobrevenda e sobrecompra.
O sistema abre uma posição comprada quando o RSI cai abaixo do nível de sobrevenda e uma posição vendida quando o RSI sobe acima do nível de sobrecompra.
As posições são fechadas quando o RSI retorna a um limiar médio ou quando os níveis de stop-loss, take profit ou trailing stop são acionados.

## Detalhes

- **Critérios de entrada**: RSI cruzando abaixo de `Oversold` para compras ou acima de `Overbought` para vendas.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: RSI cruzando `ExitLevel`, stop-loss, take profit ou trailing stop.
- **Stops**: Sim, stop-loss fixo, take profit e trailing stop opcional.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

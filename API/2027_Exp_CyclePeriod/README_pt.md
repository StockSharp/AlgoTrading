# Estratégia Exp CyclePeriod
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador CyclePeriod para detectar reversões de ciclo de mercado. Ela abre posições compradas quando o indicador sobe e posições vendidas quando cai, fechando posições opostas de acordo.

## Detalhes

- **Critérios de entrada:**
  - **Comprado**: CyclePeriod está subindo e o valor atual está acima do anterior.
  - **Vendido**: CyclePeriod está caindo e o valor atual está abaixo do anterior.
- **Comprado/Vendido**: Comprado e Vendido.
- **Critérios de saída:**
  - Fechar vendido quando CyclePeriod vira para cima.
  - Fechar comprado quando CyclePeriod vira para baixo.
- **Stops**: Usa take profit e stop loss em unidades de preço.
- **Valores padrão:**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true.
  - `SellPosClose` = true.
- **Filtros:**
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: CyclePeriod
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 6 horas
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

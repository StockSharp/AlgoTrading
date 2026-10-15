# Estratégia SEMA SDI Webhook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento de EMA suavizada e confirmação pelo índice direcional suavizado.
Compra quando +DI > -DI e EMA rápida > EMA lenta. Vende quando -DI > +DI e EMA rápida < EMA lenta.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `+DI > -DI && FastEMA > SlowEMA`
  - Vendido: `+DI < -DI && FastEMA < SlowEMA`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Take profit, stop-loss, trailing
- **Stops**: TP, SL, trailing
- **Valores padrão**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, Directional Index
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

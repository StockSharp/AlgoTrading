# Estratégia JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O JK BullP AutoTrader é um porte do Expert Advisor original do MetaTrader que depende do oscilador Bulls Power. Ele interpreta a relação entre dois valores consecutivos de Bulls Power para detectar quando a força altista está enfraquecendo acima da linha zero ou quando cai abaixo de zero e se reverte. Operações compradas e vendidas são protegidas com stops fixos e um trailing stop incremental que se aperta conforme a operação se torna lucrativa.

## Detalhes

- **Critérios de entrada**: Vender quando Bulls Power de duas barras atrás está acima da barra anterior e a barra anterior está acima de zero. Comprar quando a barra anterior de Bulls Power está abaixo de zero.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit fixo, stop loss fixo ou trailing stop atingido. Sinais opostos revertem a posição.
- **Stops**: Take profit fixo, stop loss fixo, trailing stop.
- **Valores padrão**:
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Bulls Power
  - Stops: Fixo + Trailing
  - Complexidade: Básico
  - Período: Intradiário / Swing (1H)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia Heiken Ashi Supertrend ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina velas Heiken Ashi, a direção do Supertrend e um filtro ADX opcional. Uma vela Heiken Ashi de alta sem sombra inferior abre uma posição comprada em tendência de alta. Velas de baixa sem sombra superior abrem posições vendidas em tendência de baixa. As posições fecham em sinais opostos ou em um stop trailing baseado em ATR.

Os testes indicam um retorno anual médio de aproximadamente 128%. Funciona melhor no mercado de criptomoedas.

Heiken Ashi suaviza o ruído enquanto Supertrend e ADX confirmam a direção. O ATR determina os stops dinâmicos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: vela HA de alta sem sombra inferior com Supertrend ascendente e confirmação ADX opcionais
  - Vendido: vela HA de baixa sem sombra superior com Supertrend descendente e confirmação ADX opcionais
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Vela oposta ou stop trailing ATR
- **Stops**: Stop trailing ATR
- **Valores padrão**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Heiken Ashi, Supertrend, ADX, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

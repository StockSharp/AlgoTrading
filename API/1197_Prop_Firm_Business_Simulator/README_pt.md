# Simulador de Negócios de Prop Firm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que simula o gerenciamento de risco de uma prop firm usando rompimentos do Canal Keltner com dimensionamento de posição baseado no risco por operação.

O método coloca ordens stop nos limites do canal. A quantidade é calculada de modo que a distância entre as bandas represente o percentual escolhido do capital da conta.

## Detalhes

- **Critérios de entrada**: O preço rompe as bandas do Canal Keltner.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Rompimento da banda oposta.
- **Stops**: Sim.
- **Valores padrão**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 10
  - `Multiplier` = 2m
  - `RiskPerTrade` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Keltner, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

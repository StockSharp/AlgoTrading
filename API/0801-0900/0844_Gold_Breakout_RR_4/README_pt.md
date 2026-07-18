# Estratégia de Rompimento Gold RR4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Gold Breakout RR4 negocia rompimentos do Canal de Donchian no ouro com filtros de volume e tendência LWTI. Apenas uma operação por dia dentro de uma sessão especificada, usando risco/recompensa fixo de 4:1.

## Detalhes

- **Critérios de entrada**: o preço rompe o canal Donchian com volume acima da média e confirmação LWTI dentro da sessão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop e alvo fixos por risco/recompensa
- **Stops**: Sim
- **Valores padrão**:
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian Channel, SMA, WMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

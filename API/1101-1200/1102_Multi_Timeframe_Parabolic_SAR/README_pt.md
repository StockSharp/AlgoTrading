# Parabolic SAR Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina sinais de Parabolic SAR de múltiplos períodos. Operações compradas são acionadas quando o preço permanece acima dos níveis SAR selecionados pelos parâmetros. Operações vendidas aparecem quando o preço cai abaixo dos SARs escolhidos. Stop loss, stop trailing e alvo de lucro opcionais estão disponíveis.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço acima do SAR de acordo com a configuração `LongSource`.
  - **Vendido**: Preço abaixo do SAR de acordo com a configuração `ShortSource`.
- **Critérios de saída**:
  - Cruzamento oposto do SAR ou ativação de proteções.
- **Indicadores**:
  - Parabolic SAR no período atual
  - Parabolic SAR opcional em períodos superiores e inferiores
- **Stops**: Stop loss, stop trailing e alvo de lucro opcionais via StartProtection.
- **Valores padrão**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Período: principal 5m, superior 1d, inferior 1m
  - Indicadores: Parabolic SAR
  - Stops: opcional
  - Complexidade: Moderado

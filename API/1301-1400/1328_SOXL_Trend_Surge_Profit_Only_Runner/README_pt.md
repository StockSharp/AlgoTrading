# Estratégia SOXL de Impulso de Tendência Somente Lucros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em operações compradas quando o preço está em tendência acima da EMA 200 e o SuperTrend é altista. Requer ATR em alta, volume acima da média, um filtro de sessão e que o preço se mantenha fora de um pequeno buffer de EMA. O sistema realiza lucro parcial em um alvo baseado em ATR e faz trailing da posição restante com um stop ATR.

## Detalhes

- **Critérios de entrada**: preço acima da EMA, SuperTrend para cima, volume acima da média, ATR subindo, fora do buffer EMA, hora entre 14–19 horas, cooldown após saídas
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: realização parcial de 50% no alvo ATR e trailing stop do restante
- **Stops**: Trailing
- **Valores padrão**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: EMA, ATR, SuperTrend, Volume
  - Stops: Trailing
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

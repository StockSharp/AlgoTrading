# Estratégia do Modelo Captain Backtest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Rastreia o intervalo de preço da sessão inicial para estabelecer um viés diário. Opera rompimentos durante a janela de negociação após uma retração.

## Detalhes

- **Viés**: O máximo ou mínimo do intervalo matutino define o viés comprado ou vendido.
- **Entrada**: Rompimento acima/abaixo da vela anterior quando as condições de retração forem atendidas.
- **Comprado/Vendido**: Ambos.
- **Saída**: Risco/recompensa fixo ou fim da janela de negociação.
- **Stops**: Distância fixa em pontos.
- **Valores padrão**:
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75

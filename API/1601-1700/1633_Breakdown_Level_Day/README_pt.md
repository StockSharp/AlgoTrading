# Estratégia de Rompimento de Nível Diário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens stop pendentes acima e abaixo do intervalo intradiário em um horário específico do dia. O objetivo é capturar rompimentos quando o preço se move além da máxima ou mínima da sessão inicial. Regras opcionais de stop loss, take profit, ponto de equilíbrio e trailing stop gerenciam a posição aberta.

## Detalhes

- **Entrada**: No horário `OrderTime`, um buy stop é colocado acima da máxima do dia mais `Delta` ticks e um sell stop abaixo da mínima do dia menos `Delta` ticks.
- **Saída**: As ordens de stop-loss e take-profit são colocadas junto com a ordem de rompimento. Ponto de equilíbrio e trailing stop podem atualizar o stop protetor.
- **Indicadores**: Nenhum.
- **Período**: Velas de 1 minuto por padrão.
- **Risco**: O tamanho da posição é obtido da propriedade `Volume` da estratégia.

## Parâmetros

- `OrderTime` — horário do dia em que as ordens pendentes são enviadas.
- `Delta` — distância dos limites do intervalo em ticks.
- `StopLoss` — distância do stop protetor em ticks.
- `TakeProfit` — distância do alvo de lucro em ticks.
- `BreakEven` — mover o stop para a entrada após este lucro em ticks.
- `Trailing` — distância do trailing stop em ticks.
- `CandleType` — tipo de vela usado para os cálculos.

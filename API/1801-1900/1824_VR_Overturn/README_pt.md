# Estratégia VR Overturn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia VR Overturn** implementa uma lógica simples de martingale/anti-martingale.
Ela sempre mantém uma única posição e, ao ser fechada, abre imediatamente uma nova
com base no resultado da operação anterior.

## Lógica da estratégia

1. Abrir a primeira posição de acordo com `StartSide` com volume `StartVolume`.
2. Anexar stop-loss e take-profit usando deslocamentos em pontos.
3. Quando a posição fecha:
   - Calcular o lucro da última operação.
   - Para o modo **Martingale**:
     - Após uma operação lucrativa, redefinir o volume para `StartVolume` e manter a mesma direção.
     - Após uma operação com perda, multiplicar o volume por `Multiplier` e inverter a direção.
   - Para o modo **AntiMartingale**:
     - Após uma operação lucrativa, multiplicar o volume por `Multiplier` e manter a mesma direção.
     - Após uma operação com perda, redefinir o volume para `StartVolume` e inverter a direção.
4. Abrir a próxima posição usando a direção e o volume calculados.

O processo se repete indefinidamente enquanto a estratégia está em execução.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Mode` | Modo de trading: `Martingale` ou `AntiMartingale`. |
| `StartSide` | Lado da primeira operação (`Buy` ou `Sell`). |
| `TakeProfit` | Valor de take-profit em pontos a partir do preço de entrada. |
| `StopLoss` | Valor de stop-loss em pontos a partir do preço de entrada. |
| `StartVolume` | Volume inicial usado para a primeira ordem. |
| `Multiplier` | Multiplicador aplicado ao volume após lucro ou perda. |

## Notas

- Ordens de proteção são registradas como ordens stop e limitadas.
- Apenas uma posição existe a qualquer momento.
- A estratégia não usa nenhum indicador de mercado.

# Estratégia legada de lógica difusa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o consultor especialista em "lógica difusa" MetaTrader de 2007 em StockSharp. Combina várias ferramentas do Bill Williams
com osciladores de momento e os avalia através de uma tabela de pontuação difusa. Somente quando a pontuação agregada mostra forte alta
Com um consenso de baixa, o sistema abre uma nova posição. Um stop-loss fixo e um trailing stop opcional refletem o trade original
regras de gerenciamento.

## Lógica de negociação

1. Construa a conta Williams Alligator (mandíbula, dentes, lábios) usando médias móveis suavizadas e calcule o spread *Gator* como o su
m de distâncias absolutas entre as linhas.
2. Calcule Williams%R (período 14), DeMarker (período 14) e RSI (período 14) nas mesmas velas.
3. Derive o Accelerator Oscillator (AC) da sequência Awesome Oscillator e rastreie até cinco barras consecutivas para detectar AC
estrias de aceleração.
4. Cada indicador alimenta uma tabela de associação difusa de cinco níveis com pontos de interrupção predefinidos copiados do código original.
5. As somas ponderadas das associações produzem um valor de decisão entre 0 e 1:
   - Valores **> 0,75** indicam consenso altista e desencadeiam entradas longas.
   - Valores **< 0,25** indicam consenso de baixa e desencadeiam entradas curtas.
6. Apenas uma posição pode ser aberta por vez. Os batentes de proteção são fixados imediatamente após a entrada.

## Gerenciamento de posição

- **Stop-loss**: distância fixa em etapas de preço (parâmetro `Stop Loss (points)`).
- **Parada final**: Opcional; quando ativado, ele segue o stop de proteção pelo número especificado de etapas de preço.
- **Gerenciamento de dinheiro**: dimensionamento opcional baseado em saldo que imita a fórmula MetaTrader `Volume = (Saldo * (PercentMM + DeltaM
M) - Saldo Inicial * DeltaMM) / 10000`.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Candle Type` | Série de dados de velas usada para análise. |
| `Long Threshold` | Nível de decisão que deve ser excedido para abrir uma posição longa. |
| `Short Threshold` | Nível de decisão que deve ser cruzado para abrir uma posição curta. |
| `Stop Loss (points)` | Distância do stop loss inicial em etapas de preço. |
| `Trailing Stop (points)` | Distância do trailing stop nas etapas de preço; defina como `0` para desativar. |
| `Fixed Volume` | Volume de negociação quando a gestão de dinheiro está desativada. |
| `Use Money Management` | Ativa a fórmula de gerenciamento de dinheiro no estilo MetaTrader. |
| `Percent MM` | Porcentagem do saldo da conta utilizada na fórmula de gestão de dinheiro. |
| `Delta MM` | Compensação percentual adicional para a fórmula de gestão de dinheiro. |
| `Initial Balance` | Saldo de referência utilizado pela fórmula de gestão de dinheiro. |

## Notas

- A estratégia usa apenas velas concluídas (`CandleStates.Finished`) para evitar repintura.
- Todos os níveis e pesos dos indicadores seguem o consultor especialista original, preservando o seu comportamento.
- Para executar o sistema intradiário, ajuste o prazo e os limites da vela para refletir a volatilidade desejada.

# Estratégia Cycle Lines
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Cycle Lines é a versão para StockSharp do expert advisor do MetaTrader "Cycle Lines". O script original combinava desenho no gráfico com botões manuais de negociação. Esta versão se concentra na lógica automatizada de negociação que acompanhava esses controles. A estratégia negocia cruzamentos da linha MACD, mantém o risco rigidamente controlado por limites absolutos de stop-loss e take-profit, e oferece suporte a gestão de break-even e trailing stop.

Quando a linha MACD cruza acima da sua linha de sinal, a estratégia abre uma posição comprada. Quando a linha cruza abaixo da linha de sinal, ela abre uma posição vendida. Operações abertas são fechadas se o indicador virar para a direção oposta ou se qualquer regra de proteção (stop-loss, take-profit, break-even ou trailing stop) for acionada.

## Regras de negociação

1. **Condições de entrada**
   - **Comprado:** MACD cruza acima da linha de sinal na série de candles selecionada.
   - **Vendido:** MACD cruza abaixo da linha de sinal na série de candles selecionada.
   - As entradas só são avaliadas depois que o indicador está totalmente formado e a estratégia está conectada e autorizada a negociar.
2. **Condições de saída**
   - Cruzamento MACD oposto.
   - Stop-loss atingido.
   - Take-profit atingido.
   - Nível de proteção break-even tocado.
   - Nível de trailing stop tocado.

## Parâmetros

| Nome | Descrição | Padrão | Observações |
| ---- | --------- | ------ | ----------- |
| `Volume` | Volume da ordem por operação. | `1` | Deve ser positivo. |
| `MacdFastPeriod` | Período da EMA rápida dentro do cálculo MACD. | `12` | Otimizável. |
| `MacdSlowPeriod` | Período da EMA lenta dentro do MACD. | `26` | Otimizável. |
| `MacdSignalPeriod` | Período da linha de sinal do MACD. | `9` | Otimizável. |
| `StopLoss` | Distância absoluta de preço para o stop de proteção. | `0` | Desabilitado quando definido como `0`. |
| `TakeProfit` | Distância absoluta de preço para o alvo de take-profit. | `0` | Desabilitado quando definido como `0`. |
| `TrailingOffset` | Distância mantida entre o melhor preço favorável e o trailing stop. | `0` | Desabilitado quando definido como `0`. |
| `BreakEvenTrigger` | Distância de lucro necessária antes de mover o stop para break-even. | `0` | Desabilitado quando definido como `0`. |
| `BreakEvenOffset` | Offset adicional aplicado ao nível de break-even. | `0` | Permite travar algum lucro extra acima/abaixo da entrada. |
| `CandleType` | Série de candles usada para os cálculos dos indicadores. | Candles de período de `15` minutos | Aceita qualquer `DataType` suportado pelo StockSharp. |

## Gestão da posição

- **Stop-loss / take-profit:** Ambas as verificações avaliam os extremos intrabar (máxima/mínima) dos candles concluídos, garantindo que a saída respeite a distância absoluta configurada a partir do preço de entrada.
- **Break-even:** Quando o preço se move a favor por pelo menos `BreakEvenTrigger`, a estratégia arma um stop em `entry ± BreakEvenOffset`. Qualquer retração que toque esse nível fecha a posição.
- **Trailing stop:** Para operações compradas, o maior preço atingido é monitorado. O nível do stop acompanha a máxima menos `TrailingOffset`. Para operações vendidas, a lógica espelha o comportamento em torno do menor preço.

## Notas de uso

- A estratégia negocia uma única posição por vez. Sinais consecutivos não farão pirâmide em uma posição existente.
- Os parâmetros são expostos como objetos `StrategyParam<T>` e, portanto, suportam otimização dentro do StockSharp.
- Para reproduzir o comportamento padrão do EA original, configure os períodos MACD como `12 / 26 / 9` e ajuste os parâmetros de risco de acordo com os valores em pips desejados.

## Diferenças em relação à versão MQL

- Recursos de desenho no gráfico e botões manuais BUY/SELL foram omitidos porque o StockSharp lida com visualização de maneira diferente.
- Todas as regras de proteção foram reescritas para operar em dados de candles em vez de eventos de tick do MetaTrader, mantendo-as compatíveis com a API de alto nível do StockSharp.
- A lógica de trailing e break-even é simétrica para operações compradas e vendidas, por clareza e robustez.

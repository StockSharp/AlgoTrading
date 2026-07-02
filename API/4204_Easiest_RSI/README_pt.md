# Estratégia RSI mais fácil (ID 4204)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertido do consultor especialista MetaTrader 4 **"Mais fácil RSI"** localizado em `MQL/9827/Easiest_RSI.mq4`.

## Visão geral

O EA original abre negociações quando o Índice de Força Relativa (RSI) sai das zonas de sobrevenda/sobrecompra e, opcionalmente, adiciona duas posições extras na mesma direção enquanto o preço continua se movendo favoravelmente. Cada ordem usa o mesmo volume, um stop loss fixo e um trailing stop que avança em pequenos passos quando a negociação atinge grandes lucros.

Esta porta StockSharp mantém o comportamento no nível da estratégia:

- RSI(14) calculado na série de velas configurada aciona os sinais.
- As negociações longas são acionadas quando RSI ultrapassa o limite de sobrevenda; as posições vendidas aparecem em cruzamentos descendentes através do limite de sobrecompra.
- A escala de posição imita a lógica de média MT4 adicionando um novo pedido sempre que o preço avança em `StepPips`, limitado por `MaxEntries`.
- Os stops iniciais e finais são gerenciados internamente com distâncias de preços medidas em pips (ajustadas automaticamente para cotações de câmbio de 4/5 dígitos).
- Todo o estado (histórico RSI, preços da última entrada, trailing stops) é armazenado em campos primitivos para seguir as diretrizes da estrutura.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `LotSize` | `1` | Volume de cada ordem de mercado. |
| `StopLossPips` | `50` | Parada protetora inicial em pips (definida como zero para desabilitar). |
| `TrailingStopPips` | `50` | Distância do trailing-stop em pips; zero desativa o rastreamento. |
| `StepPips` | `20` | Movimento favorável mínimo antes de uma posição adicional ser adicionada. |
| `RsiPeriod` | `14` | RSI comprimento. |
| `OversoldLevel` | `30` | Nível RSI que deve ser cruzado para cima para acionar entradas longas. |
| `OverboughtLevel` | `70` | Nível RSI que deve ser cruzado para baixo para acionar entradas curtas. |
| `MaxEntries` | `3` | Número máximo de entradas sequenciais por direção (correspondendo ao limite MT4). |
| `CandleType` | `TimeFrame(5m)` | Tipo de vela/período usado para calcular RSI. |

Todas as distâncias expressas em pips são convertidas em preços absolutos usando o valor do instrumento `Step`. Para símbolos FX de 5 dígitos, o auxiliar dobra a etapa para que entradas como `50` sejam iguais a 5,0 pips, refletindo a orientação original EA.

## Lógica de negociação

1. **Detecção de sinal** – A estratégia observa apenas velas acabadas. Ele armazena as duas últimas leituras RSI para replicar as chamadas MT4 `iRSI(..., 1)` e `iRSI(..., 2)`. Atravessa o fogo `OversoldLevel` ou `OverboughtLevel` assim que a nova vela fecha.
2. **Entradas primárias** – Quando ocorre uma linha plana e de alta, uma ordem de compra ao mercado é enviada; cruzes de baixa quando planas acionam uma ordem de venda.
3. **Scaling in** – Enquanto uma posição está aberta, a estratégia compara o último fechamento/máximo (longo) ou fechamento/mínimo (curto) com o preço do preenchimento mais recente. Cada vez que o preço se move pelo menos `StepPips` a favor, uma nova ordem com tamanho `LotSize` é enviada, até `MaxEntries` posições totais nessa direção.
4. **Stop-loss** – Em cada preenchimento, um stop inicial é recalculado como o preço da posição menos/mais `StopLossPips`. A parada agregada mantém a distância mais distante (mais conservadora) para que toda a posição permaneça protegida.
5. **Trailing** – Depois que a negociação progride, o stop é avançado para mais perto usando a máxima da vela (longas) ou a mínima (vendidas). Um pequeno buffer equivalente a cinco etapas de preço mínimo emula o requisito MT4 `OrderStopLoss() + 5*Point` antes que o stop seja movido.
6. **Saída** – Quando o preço atinge o nível de stop gerenciado, a posição é fechada no mercado. Nenhuma meta de lucro é usada além do trailing stop.

## Notas de implementação

- Os pedidos são enviados por meio do pipeline `SubscribeCandles().Bind(...)` de alto nível e auxiliares de pedidos de mercado (`BuyMarket` / `SellMarket`).
- A estratégia mantém `_longOrderPending` / `_shortOrderPending` e sinalizadores de saída para evitar inundar a exchange com solicitações duplicadas enquanto uma ordem de mercado aguarda confirmação.
- `StartProtection` não é invocado porque toda a lógica de proteção é codificada explicitamente para corresponder ao comportamento do MT4.
- Como StockSharp funciona com posições líquidas, o trailing stop é aplicado à exposição agregada. Isso significa que quando múltiplas entradas estão abertas, todos os lotes saem juntos assim que o stop combinado é tocado. O EA original moveu o stop de cada pedido individualmente; a abordagem agregada mantém o controlo do risco, mas pode fechar o cabaz um pouco mais cedo. A diferença está documentada para fins de transparência.

## Dicas de uso

1. Atribua a segurança e o conector desejados e defina `CandleType` para corresponder ao período de tempo que você deseja negociar (por exemplo, velas EURUSD de 5 minutos, como nos comentários da fonte).
2. Ajuste os parâmetros baseados em pip de acordo com a volatilidade do instrumento. Lembre-se de multiplicar os padrões por 10 se preferir trabalhar em pontos brutos para cotações de 5 dígitos, refletindo a orientação do MT4.
3. Opcional: ajuste `MaxEntries` e `StepPips` para gerenciar a agressividade da estratégia em negociações vencedoras.
4. Execute primeiro a estratégia na negociação de papel para validar as conversões de pip e o comportamento de rastreamento nos símbolos do seu corretor.

## Arquivos

- `CS/EasiestRsiStrategy.cs` – Implementação da estratégia.
- `README.md` – Este documento.
- `README_zh.md` – tradução chinesa.
- `README_ru.md` – Tradução russa.

A implementação do Python é omitida intencionalmente conforme solicitado.

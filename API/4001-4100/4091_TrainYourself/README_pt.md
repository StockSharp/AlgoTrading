# Estratégia Treine-se
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader 4 **TrainYourself-V1_1-1**. Ele recria a lógica de construção e breakout do canal enquanto substitui os botões gráficos do script MT4 por chamadas de método explícitas. O algoritmo reconstrói continuamente um canal de preços no estilo Donchian e dispara uma negociação assim que o preço escapa do canal após a primeira consolidação dentro dele.

## Lógica de negociação

1. **Construção do canal**
   - Um indicador `DonchianChannels` com períodos `ChannelLength` é avaliado em cada vela finalizada do `CandleType` selecionado.
   - As bandas superior e inferior brutas são expandidas com um buffer extra semelhante a MetaTrader: `BufferPoints` multiplicado pelo instrumento `PriceStep`. Isso reproduz o script original que inicialmente colocou as linhas de tendência a 50 pontos da oferta/venda atual antes de deslizá-las sobre máximos e mínimos recentes.
   - Os valores `UpperBand`/`LowerBand` resultantes são expostos como propriedades somente leitura para que possam ser exibidos em painéis personalizados.

2. **Condição de armamento**
   - O mecanismo de breakout permanece desarmado enquanto uma posição está aberta ou quando `EnableTrendTrade` é falso.
   - Quando não há posição, o preço deve fechar dentro do canal com uma margem adicional de `ActivationPoints` * `PriceStep` de ambos os limites. Só então `_isArmed` se torna `true`, imitando o MetaTrader sinalizador `q=1` que foi definido quando o preço voltou para o canal.

3. **Execução de breakout**
   - Uma vez armado, um fechamento igual ou superior a `UpperBand` coloca uma ordem de compra de mercado (se `AllowBuyOpen` estiver ativado). Um fechamento igual ou inferior a `LowerBand` coloca uma ordem de venda a mercado (respeitando `AllowSellOpen`).
   - Depois que uma ordem é colocada, a estratégia se desarma até que o preço entre novamente no canal sem qualquer posição aberta.

4. **Gerenciamento de riscos**
   - `StartProtection` configura ordens de proteção automáticas. As distâncias são calculadas multiplicando `TakeProfitPoints` e `StopLossPoints` pelo `PriceStep` atual. Se o corretor não relatar uma etapa, um substituto de `0.0001` será usado, correspondendo ao comportamento de MetaTrader `Point`.

5. **Controles manuais**
   - Os rótulos MT4 (`BUY_TRIANGLE`, `SELL_TRIANGLE`, `CLOSE_ORDER`) são substituídos por três métodos públicos: `TriggerManualBuy()`, `TriggerManualSell()` e `ClosePositionManually()`. Eles respeitam `AllowBuyOpen`/`AllowSellOpen`, verificam o status da conexão via `IsFormedAndOnlineAndAllowTrading()` e também desarmam a lógica de breakout para que as negociações manuais não acionem imediatamente entradas automatizadas.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | `30m` período de tempo | Assinatura de vela primária usada para todos os cálculos. |
| `ChannelLength` | `20` | Número de velas analisadas pelo canal Donchian. |
| `BufferPoints` | `50` | MetaTrader pontos extras adicionados no último fechamento antes de finalizar o canal. |
| `ActivationPoints` | `2` | Margem (em pontos) que o preço deve manter longe das bordas do canal antes que um rompimento possa ser armado. |
| `StopLossPoints` | `100` | Distância stop-loss em pontos; convertido em preço absoluto multiplicando por `PriceStep`. |
| `TakeProfitPoints` | `100` | Distância de take-profit em pontos; convertido em preço absoluto usando `PriceStep`. |
| `EnableTrendTrade` | `true` | Permite negociação de breakout automática. Quando `false` apenas os métodos auxiliares manuais podem abrir/fechar posições. |
| `Volume` | `1` | Tamanho do pedido para negociações automáticas e manuais. |

## Notas de uso

- O consultor especialista original exigia arrastar ícones no gráfico para (re)construir linhas de tendência. Em StockSharp o canal é reconstruído automaticamente a cada vela, portanto nenhuma atualização manual é necessária.
- Como a estratégia expõe `UpperBand`, `LowerBand` e `IsArmed`, os painéis ou widgets de IU podem replicar o feedback visual original sem depender de objetos gráficos.
- Os níveis de stop-loss e take-profit são opcionais. Defina os parâmetros correspondentes como `0` para desativar as ordens de proteção, espelhando o comportamento MetaTrader onde as rotinas de modificação foram ignoradas quando o valor externo era zero.
- As entradas manuais respeitam o mesmo parâmetro `Volume` e beneficiam automaticamente das distâncias de proteção configuradas.
- Para redefinir o estado de rompimento manualmente, chame `ClosePositionManually()` (que também limpa `IsArmed`) ou espere que o preço entre novamente no canal para que a condição de armar seja satisfeita novamente.

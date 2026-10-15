# Estratégia AnyRange Cloud Tail System Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento do consultor especialista **Exp_i-AnyRangeCldTail_System_Tm_Plus.mq5** usando a API de alto nível do StockSharp. Ela constrói uma range intradiária personalizada entre dois horários definidos pelo usuário, aguarda rompimentos além dessa range e agenda ordens um número configurável de barras após o rompimento para que os sinais estejam alinhados com a lógica de temporização MQL original.

A estratégia é projetada para negociações compradas e vendidas. Ela expõe parâmetros que controlam permissões de rompimento, distâncias de stop-loss/take-profit em passos de preço, o período de manutenção e a janela de cálculo do indicador. Além disso, uma saída baseada em tempo fecha posições que permanecem abertas por mais tempo que o número configurado de minutos, correspondendo à lógica protetora do consultor especialista fonte.

## Lógica de negociação

1. **Construção da range**
   - Dois carimbos de tempo (`RangeStartTime` e `RangeEndTime`) definem a janela de sessão usada para calcular a range de referência.
   - Para cada dia completado a estratégia registra a máxima mais alta e a mínima mais baixa entre esses carimbos de tempo. Se `RangeStartTime` for maior que `RangeEndTime`, a janela automaticamente se estende pela meia-noite, assim como o indicador original.
   - A range completada mais recente é reutilizada até que uma nova range diária seja completada.

2. **Detecção de rompimento**
   - Cada candle finalizado é comparado com a range armazenada.
   - Candles que fecham acima da máxima da range recebem os mesmos códigos de cor (2 ou 3) que o indicador MQL, enquanto candles que fecham abaixo da mínima da range recebem códigos 0 ou 1. Candles dentro da range são marcados com código 4 (sem sinal).
   - O parâmetro `SignalBar` desloca o ponto de inspeção: a estratégia avalia o candle que tem `SignalBar + 1` barras de idade e confirma que o candle mais recente (`SignalBar`) não carrega a mesma cor. Isso reproduz a confirmação atrasada usada pelo EA para acionar ordens uma barra após o candle de rompimento.

3. **Entradas**
   - **Comprado**: permitido quando `AllowBuyEntry` é verdadeiro e uma cor altista (2 ou 3) é detectada na barra de sinal enquanto a barra seguinte não repete a cor de rompimento.
   - **Vendido**: permitido quando `AllowSellEntry` é verdadeiro e uma cor baixista (0 ou 1) é detectada na barra de sinal enquanto a barra seguinte não repete a cor de rompimento.
   - Se uma posição oposta estiver aberta, seu volume é adicionado à nova ordem de mercado para que a posição vire imediatamente, emulando o comportamento das funções auxiliares em `TradeAlgorithms.mqh`.

4. **Saídas**
   - **Sinal oposto**: se `AllowBuyExit` estiver habilitado, uma cor baixista (0 ou 1) na barra de sinal fecha posições compradas. Se `AllowSellExit` estiver habilitado, uma cor altista (2 ou 3) fecha posições vendidas.
   - **Saída por tempo**: quando `UseTimeExit` é verdadeiro, as posições são liquidadas após `ExitAfterMinutes` minutos desde a entrada, correspondendo ao loop MQL que escaneia posições e as fecha após `nTime` minutos.
   - **Stops/Alvos**: as proteções opcionais de stop-loss e take-profit são configuradas via `StopLossPoints` e `TakeProfitPoints`. Os valores são convertidos em distâncias de preço usando o passo de preço do instrumento, espelhando a configuração original baseada em pontos.

5. **Controles de risco**
   - As ordens usam o `OrderVolume` configurado (tamanho base expresso em unidades de volume do instrumento). O tamanho da ordem é aplicado em cada chamada `BuyMarket`/`SellMarket` e ajustado ao virar posições.
   - O stop-loss e o take-profit são gerenciados pelo auxiliar integrado `StartProtection`, que registra proteções OCO logo após a estratégia iniciar.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Tamanho base da ordem para novas posições. | `0.1` |
| `AllowBuyEntry` | Permitir entradas compradas em rompimentos altistas. | `true` |
| `AllowSellEntry` | Permitir entradas vendidas em rompimentos baixistas. | `true` |
| `AllowBuyExit` | Fechar posições compradas em rompimentos baixistas. | `true` |
| `AllowSellExit` | Fechar posições vendidas em rompimentos altistas. | `true` |
| `UseTimeExit` | Habilitar saída baseada em tempo. | `true` |
| `ExitAfterMinutes` | Tempo de manutenção em minutos antes da saída por tempo ser acionada. | `1500` |
| `StopLossPoints` | Distância de stop-loss em passos de preço. Usar `0` para desabilitar. | `1000` |
| `TakeProfitPoints` | Distância de take-profit em passos de preço. Usar `0` para desabilitar. | `2000` |
| `SignalBar` | Número de barras atrás inspeccionadas para detecção de rompimento (corresponde ao `SignalBar` do MQL). | `1` |
| `RangeLookbackDays` | Número máximo de sessões passadas escaneadas para encontrar uma range completada. Definir `0` para sempre usar apenas a range mais recente. | `1` |
| `RangeStartTime` | Início da janela de construção da range (TimeSpan). | `02:00` |
| `RangeEndTime` | Fim da janela de construção da range (TimeSpan). | `07:00` |
| `CandleType` | Tipo de dados/período de candle usado para cálculos. | `30 minutos` |

## Notas de implementação

- A classe usa `SubscribeCandles` e o pipeline orientado por eventos `WhenNew` para processar apenas candles finalizados, garantindo que as decisões correspondam ao consultor especialista MQL que dependia de verificações `IsNewBar`.
- Os valores da range são armazenados em structs leves e o algoritmo evita LINQ sobre coleções completas para cumprir as diretrizes do projeto.
- A saída por tempo armazena o carimbo de tempo de entrada para a direção atualmente aberta, refletindo como o código-fonte iterava pelas posições abertas.
- O volume da ordem é sincronizado com a propriedade base `Strategy.Volume` para que a UI do StockSharp reflita o tamanho configurado.
- O código contém comentários em inglês que explicam cada seção principal para facilitar a manutenção e personalização adicional.

## Dicas de uso

- Certifique-se de que o feed de dados fornece candles alinhados com o `CandleType` escolhido. A detecção de rompimento depende de candles completados; barras baseadas em ticks ou parcialmente formadas não devem ser processadas.
- Ao negociar mercados com diferentes sessões de negociação, ajuste `RangeStartTime` e `RangeEndTime` para cobrir o período de acumulação que melhor corresponde ao instrumento subjacente.
- Se o instrumento tiver um passo de preço irregular, verifique a conversão de `StopLossPoints`/`TakeProfitPoints` inspeccionando as ordens de proteção geradas no gráfico ou no registro de ordens.
- Reduza `ExitAfterMinutes` ao operar em períodos mais rápidos para evitar manter posições por mais tempo do que o pretendido.

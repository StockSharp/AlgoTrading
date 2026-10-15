# Stop Loss Dinâmico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O consultor especialista MetaTrader original "Dynamic Stop Loss" não abre novas negociações por conta própria. Em vez disso, observa as posições de mercado existentes e, assim que uma nova vela aparece, reposiciona o stop-loss protetor para que fique a uma distância fixa atrás do preço mais recente. A porta StockSharp mantém o mesmo comportamento: cada barra completada aciona um recálculo da parada de proteção para qualquer lado que esteja aberto no momento. Se não existir nenhuma posição, a estratégia simplesmente fica inativa até que uma nova posição seja detectada.

## Como funciona
1. A estratégia assina velas definidas pelo parâmetro `Candle Type` (período padrão de 1 minuto).
2. Quando uma vela fecha, o preço de fechamento é multiplicado pela distância do ponto selecionado pelo usuário. A distância é convertida de pontos no estilo MetaTrader em um delta de preço absoluto via `Security.PriceStep` (substituição para `Security.Step` e depois para `1`).
3. Se uma posição longa estiver aberta, a estratégia cancela qualquer ordem stop existente e coloca um novo stop de venda em `Close - Distance`.
4. Se uma posição curta estiver aberta, o stop é movido para `Close + Distance` usando uma ordem stop de compra.
5. Quando a posição é fechada (manualmente ou pelo stop fill), a ordem móvel é cancelada para evitar ordens de proteção obsoletas.

Isso produz a mesma distância de parada constantemente ancorada que a versão MQL, o que significa que a parada pode se aproximar e se afastar do mercado à medida que as velas flutuam.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StopLossPoints` | `800` | Distância entre o preço de mercado e o stop de proteção medido em pontos do instrumento. O valor é multiplicado por `Security.PriceStep` (substituto para `Security.Step` e depois `1`) antes de ser aplicado ao preço de fechamento. Defina como `0` para desativar o gerenciamento de parada. |
| `CandleType` | `TimeFrameCandle(00:01:00)` | Tipo de vela que define quando o stop é recalculado. Escolha um período que corresponda ao gráfico usado em MetaTrader. |

## Notas de uso
- A estratégia espera que as negociações sejam abertas por estratégias externas, operações manuais ou outros componentes. Ele apenas gerencia o stop-loss.
- Certifique-se de que os metadados de segurança (`PriceStep`, `Step`, volume) sejam preenchidos para que a conversão ponto-preço corresponda ao tamanho do tick do corretor. Instrumentos cotados com pips fracionários devem expor o passo adequado.
- Como o stop é recalculado a cada fechamento de vela, ele seguirá o preço mesmo quando o mercado se mover contra a posição. Isso reflete a lógica MetaTrader onde `OrderModify` sempre usa o último `Bid`/`Ask` menos/mais a distância configurada.
- As ordens stop criadas sempre substituem as anteriores para manter a plataforma sincronizada com o nível de proteção mais recente.

# Enviar estratégia de fechamento de pedido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Send Close Order é uma versão do consultor especialista MetaTrader 4 de 2009 "SendCloseOrder" de Vladimir Hlystov. O script original desenha quatro linhas de tendência manuais com base nos fractais de Bill Williams e abre ou fecha ordens de mercado sempre que o preço atinge um desses níveis projetados. A versão StockSharp replica a lógica de decisão com gerenciamento de linha totalmente automatizado e funciona em qualquer série de velas fornecida pela plataforma.

## Lógica de negociação

1. **Detecção fractal** – cada vela finalizada atualiza uma janela deslizante de cinco barras. Assim que a janela estiver cheia, a vela no meio é verificada em relação às condições fractais de Bill Williams. Os altos e baixos confirmados são armazenados cronologicamente.
2. **Reconstrução da linha de tendência**
   - A *linha de venda* conecta os dois últimos fractais ascendentes que são separados por um fractal descendente, formando uma inclinação de resistência.
   - *Fecho #1* é a linha de venda deslocada para cima em `15` etapas de preço (15 × `Security.PriceStep`) e atua como o longo trilho de saída.
   - A *linha de compra* conecta os dois últimos fractais descendentes que são separados por um fractal ascendente, formando uma inclinação de suporte.
   - *Fecho #2* é a linha de compra deslocada para baixo em `15` etapas de preço e atua como o trilho de saída curto.
3. **Avaliação do sinal** – as quatro linhas são extrapoladas para o carimbo de data/hora da vela finalizada. Se o preço projetado estiver dentro da faixa máxima/mínima da vela (com uma pequena tolerância de duas etapas de preço), a ação correspondente será acionada.
4. **Gerenciamento de pedidos**
   - Tocar em Close #1 ou Close #2 fecha imediatamente toda a posição via `ClosePosition()`.
   - Tocar na linha de Venda ou Compra abre uma ordem de mercado com volume `TradeVolume`, desde que a posição absoluta resultante não exceda `MaxOrders × TradeVolume`. Quando existe uma posição oposta, a ordem compensa-a primeiro e depois empilha uma nova entrada, espelhando o comportamento das contas de cobertura.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `EnableSellLine` | `true` | Permitir negociações quando a linha de resistência projetada for atingida. |
| `EnableBuyLine` | `true` | Permitir negociações quando a linha de suporte projetada for atingida. |
| `EnableCloseLongLine` | `true` | Permitir o fechamento de posições longas na linha de resistência deslocada (Fechar #1). |
| `EnableCloseShortLine` | `true` | Permitir o fechamento de posições vendidas na linha de suporte deslocada (Fechar #2). |
| `MaxOrders` | `1` | Número máximo de entradas empilhadas na direção atual. |
| `TradeVolume` | `0.1` | Volume de cada ordem de mercado individual. |
| `CandleType` | `1h` período de tempo | Série de velas usada para cálculos fractais. |

## Diferenças em relação à versão MetaTrader

- A porta StockSharp recalcula as quatro linhas toda vez que um novo fractal aparece. Em MetaTrader o usuário teve que excluir e redesenhar as linhas de tendência manualmente.
- A execução é baseada em posições líquidas agregadas; cestas longas e curtas simultâneas não são suportadas pelo modelo de portfólio padrão de StockSharp.
- A detecção de toque usa o máximo/mínimo da vela finalizada com uma tolerância de variação de preço em vez das cotações Bid/Ask instantâneas dos ticks.
- Objetos gráficos (linhas de tendência e rótulos) não são criados; o foco está nos sinais de negociação.

## Notas de uso

- A estratégia pode ser executada em qualquer instrumento que forneça velas e um `PriceStep` válido. Quando `Security.PriceStep` é zero, o código volta para `0.0001`.
- Aumente `MaxOrders` para emular o comportamento de empilhamento do EA original. Mantenha `TradeVolume` alinhado com o tamanho do lote do instrumento para evitar arredondamentos.
- O deslocamento da linha é fixado no valor histórico de 15 pontos. Ajuste o código-fonte se a entrada MetaTrader for modificada.

Somente a implementação C# é fornecida. Uma tradução Python será adicionada separadamente, se necessário.

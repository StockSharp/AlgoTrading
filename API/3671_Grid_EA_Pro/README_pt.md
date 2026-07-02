# Grade EA Estratégia Profissional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Grid EA Pro Strategy** reproduz o comportamento central do consultor especialista MetaTrader 4 original. A estratégia combina escalonamento baseado em grade com RSI ou entradas de breakout cronometradas e recursos de gerenciamento de risco virtual, como ponto de equilíbrio e trailing stops. Ele é projetado para carteiras compensadas, o que significa que funciona sempre com uma única posição líquida e compensa automaticamente a direção oposta quando uma nova negociação é aberta.

## Lógica de negociação
- **Modo de entrada** – escolha entre RSI limites, interrupções orientadas por tempo ou operação totalmente manual. No modo manual, a estratégia gerencia apenas as posições existentes e o dimensionamento da grade.
- **Filtro direcional** – restringe a negociação a posições longas, curtas ou ambas.
- **Escalonamento da grade** – após a entrada inicial, a estratégia pode adicionar posições quando o preço retrocede por um número configurável de pontos. Tanto o passo quanto o volume do pedido podem crescer geometricamente.
- **Controles de risco** – filtros virtuais de stop-loss, take-profit, ponto de equilíbrio, trailing stop e filtros de sessão refletem o comportamento original do consultor especialista.
- **Saídas de sobreposição** – os parâmetros são fornecidos para fins de integridade, mas devido ao modelo de posição compensada, ambas as direções não podem ser mantidas simultaneamente. A lógica de sobreposição, portanto, permanece inativa e os níveis são documentados para compatibilidade futura.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Mode` | Direção comercial permitida (Compra, Venda, Ambas). |
| `EntryMode` | Fonte de sinal (RSI, Pontos Fixos, Manual). |
| `RsiPeriod`, `RsiUpper`, `RsiLower` | Configuração RSI usada no modo RSI. |
| `CandleType` | Assinatura de velas para sinais e gerenciamento de risco. |
| `Distance`, `TimerSeconds` | Distância de fuga e intervalo de atualização para entradas de ponto fixo. |
| `InitialVolume`, `FromBalance`, `Risk %` | Bloco de gerenciamento de dinheiro. Se `Risk %` > 0, o tamanho da posição é derivado do patrimônio da conta e da distância do stop loss, caso contrário, um lote fixo ou baseado em saldo será usado. |
| `LotMultiplier`, `MaxLot` | Multiplicador e limite para adições à grade. |
| `Step`, `StepMultiplier`, `MaxStep` | Configurações de espaçamento de grade em pontos. |
| `OverlapOrders`, `OverlapPips` | Reservado para lógica de sobreposição protegida (desativada nesta implementação). |
| `Stop Loss`, `Take Profit` | Níveis de proteção iniciais em pontos (`-1` desativações). |
| `Break Even Stop`, `Break Even Step` | Mova o stop para o ponto de equilíbrio depois que o preço se mover na etapa definida. |
| `Trailing Stop`, `Trailing Step` | Configuração de parada móvel. |
| `Start Time`, `End Time` | Janela da sessão de negociação no horário local da plataforma (HH:mm). |

## Gráficos
Quando a área do gráfico está disponível, a estratégia traça velas de preços, a linha RSI e todas as negociações próprias, correspondendo ao layout do consultor especialista de origem.

## Notas
- A estratégia cancela automaticamente os níveis de rompimento pendentes quando eles são preenchidos ou quando a direção é desativada.
- Como StockSharp usa posições líquidas, apenas um lado do mercado pode estar aberto por vez. Abrir uma posição longa compensa as posições vendidas existentes e vice-versa.
- Certifique-se de que as propriedades do instrumento (`PriceStep`, `StepPrice`) estejam configuradas para que os parâmetros baseados em pontos correspondam às configurações originais do MT4.

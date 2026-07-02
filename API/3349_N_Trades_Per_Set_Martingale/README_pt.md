# Estratégia de N negociações por conjunto Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão direta do consultor especialista MetaTrader "N negociações por conjunto martingale + Fechamento e redefinição no aumento do patrimônio". Ele mantém a direção do mercado simples – apenas negociações longas são realizadas – mas gerencia ativamente o dimensionamento da posição por meio de uma cascata de martingale e uma redefinição baseada em ações. Uma nova negociação é aberta imediatamente após o fechamento da anterior, mantendo a estratégia constantemente engajada no mercado.

## Lógica de negociação
1. **Entradas sequenciais** – a estratégia abre uma ordem longa de mercado sempre que nenhuma posição estiver ativa. As ordens stop-loss e take-profit são anexadas logo após o preenchimento.
2. **Contabilização de ganhos/perdas** – após o fechamento de uma posição, o preço realizado é comparado com o preço de entrada. Um fechamento lucrativo aumenta o contador de ganhos, caso contrário, o contador de perdas é incrementado. Os resultados do ponto de equilíbrio são tratados como perdas, correspondendo ao EA original.
3. **Conclusão do conjunto** – o número de negociações no conjunto atual também é rastreado. Quando o contador atinge `Trades Per Set`, o ciclo é considerado completo e um dos três resultados pode acontecer:
   - **Todas as vitórias** – o volume é recalculado a partir do patrimônio atual usando `Equity Divisor` e os contadores de ciclo são zerados.
   - **Todas as perdas** – o volume é multiplicado por `Scale Factor` e os contadores de ciclo são zerados.
   - **Resultados mistos** – se o set contiver vitórias e derrotas, os contadores serão simplesmente zerados e o volume atual será preservado.
4. **Redefinição do patrimônio** – sempre que o patrimônio do portfólio cresce em pelo menos `Equity Increase`, a estratégia realiza uma redefinição global. Todos os contadores são zerados, o volume base é recalculado a partir do patrimônio líquido e a meta de patrimônio líquido avança no mesmo incremento.

Este comportamento reflete o EA original, onde os blocos comerciais eram encadeados por meio de nós lógicos fxDreema.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `Trades Per Set` | Número de negociações sequenciais que formam um ciclo martingale. |
| `Stop Loss (pips)` | Distância de stop-loss medida em etapas de preço do instrumento. Defina como zero para desativar. |
| `Take Profit (pips)` | Distância de lucro medida em etapas de preço. Defina como zero para desativar. |
| `Scale Factor` | Multiplicador aplicado ao volume de negociação após um conjunto totalmente perdedor. Valores abaixo de 1 são automaticamente fixados em 1. |
| `Equity Divisor` | Divide o patrimônio da conta para obter o tamanho do lote base após um conjunto totalmente vencedor ou uma redefinição do patrimônio. |
| `Equity Increase` | Quantidade de crescimento do capital que desencadeia a redefinição global. Defina como zero para desativar a saída baseada em capital. |

## Gestão de capital
- O volume é alinhado às restrições do instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) da mesma maneira que o EA original.
- Quando os dados de capital não estão disponíveis, o volume anterior é reutilizado, voltando para `VolumeStep` se esta for a primeira negociação.
- As distâncias de stop-loss e take-profit são convertidas em etapas de preço por meio de `PriceStep`. Se o instrumento não especificar uma etapa de preço, o valor bruto será arredondado para o número inteiro mais próximo.

## Notas de uso
- A estratégia é apenas longa, assim como o script MetaTrader. Se a corretora suportar operações a descoberto, desative-a manualmente ao executar a estratégia.
- Como as ordens stop e target são recriadas após cada preenchimento, os preenchimentos parciais são tratados normalmente – o volume restante herda as mesmas ordens de proteção.
- A redefinição do patrimônio é avaliada após cada posição fechada. Certifique-se de que a conexão do portfólio forneça valores patrimoniais atuais para que o limite de redefinição possa ser alcançado.

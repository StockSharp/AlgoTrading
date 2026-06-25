# ZigZag EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia replica a lógica original do MT5 "ZigZag EA" aguardando três pontos de oscilação ZigZag consecutivos e colocando duas ordens de stop de rompimento em torno do intervalo de oscilação anterior. A conversão utiliza a API de alto nível do StockSharp e trabalha com velas terminadas. As duas últimas oscilações completadas definem um corredor de trading, enquanto a oscilação mais recente ("room 0" na versão MQL) deve permanecer dentro desse corredor antes que a estratégia se arme com ordens pendentes. A abordagem é simétrica: prepara tanto ordens buy-stop quanto sell-stop e deixa o mercado decidir a direção do rompimento.

## Indicadores e dados de mercado
* **Highest / Lowest:** O StockSharp não expõe diretamente o indicador ZigZag do MT, portanto a conversão imita o comportamento do ZigZag rastreando os valores mais altos e mais baixos consecutivos sobre a profundidade selecionada. Mudanças de direção atualizam os buffers de oscilação internos exatamente como o EA original lendo o buffer ZigZag.
* **Velas:** a estratégia se inscreve em um tipo de vela configurável (padrão: timeframe de 1 minuto) e trabalha apenas com velas terminadas para manter a compatibilidade com backtesting e trading real.

## Lógica de trading
1. Coletar os últimos três valores de oscilação. Os dois valores anteriores determinam o corredor (`high`/`low`), e o último valor deve permanecer dentro do corredor com um pequeno buffer definido pelo nível de stop do broker.
2. Aplicar limites de tamanho do corredor (`MinCorridorPips` e `MaxCorridorPips`). Corredores muito estreitos são ignorados para evitar ruído, enquanto corredores excessivamente amplos são filtrados para evitar stops enormes.
3. Uma vez que o corredor é válido e nenhuma posição está aberta, colocar ordens pendentes simétricas:
   * **Buy stop** em `high + EntryOffsetPips`.
   * **Sell stop** em `low - EntryOffsetPips`.
4. Os stops e alvos são calculados a partir de razões de Fibonacci exatamente como na implementação MQL: `FiboStopLoss` multiplica a altura do corredor e `FiboTakeProfit` subtrai o corredor da projeção de Fibonacci selecionada. Os preços são arredondados para o tamanho do tick do instrumento para evitar rejeições.
5. Quando uma ordem pendente é acionada, a ordem pendente restante é cancelada e o stop-loss e take-profit protetores são registrados imediatamente. A lógica de trailing opcional ajusta o stop quando o preço avança `TrailingStepPips` além da distância de trailing.
6. A estratégia fecha e se rearma automaticamente quando a posição retorna a zero.

## Gestão de risco e ordens
* As ordens protetoras de stop e alvo são ordens live stop/limit, portanto o broker controla a execução e gaps são tratados naturalmente.
* A lógica de trailing stop é retirada do EA: ativa-se depois que o lucro supera `TrailingStopPips + TrailingStepPips` e depois re-registra o stop cada vez que a distância aumenta em pelo menos um passo de trailing.
* A estratégia usa o parâmetro base `Volume` da classe `Strategy` do StockSharp. Os blocos de gestão monetária da versão MQL (lote fixo vs. percentual de risco) são intencionalmente omitidos porque o dimensionamento de posição geralmente é específico do broker no StockSharp.

## Filtro de sessão
* O trading só é permitido entre `StartHour:StartMinute` e `StopHour:StopMinute`. Se o horário de stop for anterior ao horário de início, a estratégia o trata como uma sessão noturna e permite o trading através da meia-noite.
* As ordens pendentes são canceladas sempre que a sessão está fechada, refletindo o comportamento MQL que removeu ordens fora da janela permitida.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-----------|--------|
| `CandleType` | Série de velas usada para análise. | Velas de 1 minuto |
| `ZigZagDepth` | Número de velas para detecção de oscilação. | 12 |
| `EntryOffsetPips` | Offset adicionado acima/abaixo do corredor. | 5 |
| `MinCorridorPips` | Altura mínima do corredor para validar uma configuração. | 20 |
| `MaxCorridorPips` | Altura máxima do corredor permitida. | 100 |
| `FiboStopLoss` | Nível de Fibonacci usado para calcular a distância do stop-loss. | 61.8% |
| `FiboTakeProfit` | Nível de Fibonacci usado para o alvo de lucro. | 161.8% |
| `StartHour` / `StartMinute` | Início da janela de trading. | 00:01 |
| `StopHour` / `StopMinute` | Fim da janela de trading. | 23:59 |
| `TrailingStopPips` | Distância usada pelo trailing stop. | 5 |
| `TrailingStepPips` | Melhoria mínima necessária para mover o trailing stop. | 5 |
| `DrawCorridorLevels` | Se habilitado, a estratégia desenha um marcador de corredor vertical no gráfico como referência. | `false` |

## Notas de implementação
* Os valores em pips são calculados a partir do tamanho do tick do instrumento. Instrumentos com 3 ou 5 casas decimais multiplicam automaticamente o tick por 10, replicando a lógica de "ponto ajustado" usada no EA.
* O código usa métodos auxiliares de alto nível como `BuyStop`, `SellStop`, `SellLimit` e `BuyLimit`, em linha com as diretrizes do projeto.
* Os comentários são mantidos em inglês para cumprir os requisitos do repositório, enquanto a descrição detalhada é fornecida em vários idiomas nos arquivos README.
* Nenhuma porta Python é criada; a pasta contém apenas a implementação em C# conforme solicitado.

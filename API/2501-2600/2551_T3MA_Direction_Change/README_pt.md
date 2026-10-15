# Estratégia T3 MA de Mudança de Direção
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do consultor especialista original **T3MA(barabashkakvn's edition)**. O Consultor Especialista depende do indicador "T3MA-ALARM" que aplica suavização exponencial duas vezes e gera um sinal quando a linha suavizada muda de direção. O port em StockSharp mantém o mesmo conceito: cria uma média móvel exponencial de suavização dupla (EMA de EMA) e opera sempre que a inclinação dessa curva muda de descendente para ascendente ou vice-versa.

A estratégia opera apenas em velas finalizadas. Os sinais podem ser atrasados por um número configurável de barras para imitar a opção original `InpBarNumber` (atraso padrão: uma barra). As ordens são colocadas usando execução a mercado para que o portfólio alterne entre exposição comprada e vendida sem acumular múltiplas posições hedgeadas simultâneas.

## Regras de trading
1. Assinar a série de velas configurada e calcular uma EMA dos preços de fechamento. Executar uma segunda EMA sobre a saída da primeira EMA, produzindo a série suavizada usada pelo indicador.
2. Comparar o valor atual da série suavizada (opcionalmente deslocado para frente por `EMA Shift`) com o valor anterior. A inclinação é considerada de alta quando a série aumenta e de baixa quando diminui.
3. Quando a inclinação muda de baixa para alta, enfileirar um sinal de **compra**. Quando muda de alta para baixa, enfileirar um sinal de **venda**. Velas neutras inserem um sinal zero na fila para que o contador de atraso permaneça preciso.
4. Após o número configurado de velas completas (`Signal Delay`) passar, executar o sinal na fila. Uma compra atrasada fecha qualquer posição vendida aberta e entra comprado com o `Trade Volume` base. Da mesma forma, uma venda atrasada fecha uma posição comprada e entra vendido.
5. As ordens protetoras de stop-loss e take-profit são inicializadas via `StartProtection`. Ambas as distâncias são expressas em passos de preço para que se adaptem automaticamente ao tamanho do tick do instrumento selecionado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `EMA Length` | Comprimento da EMA usado para ambas as passagens de suavização. Corresponde ao parâmetro `MAPeriod` na implementação do MetaTrader. |
| `EMA Shift` | Número de barras pelo qual a EMA suavizada é deslocada antes de comparar as inclinações. Equivalente ao `MAShift` do indicador. |
| `Signal Delay` | Número de velas completadas a esperar antes de executar um sinal. Reflete `InpBarNumber`, portanto um valor de 1 opera o sinal da barra anterior. |
| `Stop Loss (steps)` | Distância do stop-loss medida em passos de preço. Definir como zero para desativar. |
| `Take Profit (steps)` | Distância do take-profit medida em passos de preço. Definir como zero para desativar. |
| `Trade Volume` | Tamanho de ordem base usado para novas entradas. Ao reverter uma posição, a estratégia adiciona o tamanho absoluto da posição atual a esse valor. |
| `Candle Type` | Tipo de dados de vela usado para cálculos (padrão: período de 5 minutos). |

## Gestão de risco
* `StartProtection` registra automaticamente níveis de stop-loss e take-profit quando a estratégia começa. Ambos os níveis seguem o tamanho do tick do instrumento e permanecem ativos durante toda a vida da estratégia.
* Os giros de posição são executados usando ordens a mercado. Quando a direção do sinal coincide com a exposição atual, nenhuma operação adicional é emitida, evitando pirâmide involuntária.
* Registros são emitidos em cada operação para rastrear o motivo e o preço de referência tomado da vela fonte.

## Diferenças em relação à versão MQL5
* O MetaTrader 5 exigia uma conta de hedge e podia acumular múltiplas posições. A versão StockSharp mantém uma única posição líquida e a reverte quando o sinal oposto é acionado.
* O processamento de sinais é baseado em velas e ocorre uma vez por vela finalizada em vez de em cada tick, o que é mais natural dentro da API de alto nível do StockSharp.
* O gerenciamento de stop-loss e take-profit é tratado via `StartProtection` em vez de enviar manualmente preços SL/TP com cada ordem.
* Comentários em inglês, parâmetros estruturados e assistentes de gráficos foram adicionados para melhor legibilidade no ambiente StockSharp.

## Notas de uso
1. Anexar a estratégia ao instrumento desejado e garantir que o tipo de vela corresponda ao período que foi usado ao otimizar o Consultor Especialista original.
2. Ajustar `EMA Length` e os parâmetros de risco para se adequar à volatilidade do instrumento. Atrasos maiores (`Signal Delay`) desaceleram as respostas e podem filtrar ruído.
3. Como a estratégia trabalha com passos de preço, verificar que a propriedade `PriceStep` do instrumento esteja configurada corretamente para que as ordens protetoras sejam colocadas em distâncias significativas.

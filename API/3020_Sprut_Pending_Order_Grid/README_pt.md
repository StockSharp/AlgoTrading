# Estratégia de Grade de Ordens Pendentes Sprut
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Grade de Ordens Pendentes Sprut** reproduz o consultor especialista do MetaTrader 5 *Sprut (edição de barabashkakvn)* dentro do framework de estratégias de alto nível do StockSharp. Ela constrói uma grade configurável de ordens pendentes de compra e venda ao redor do preço atual do mercado e gerencia o tempo de vida de cada ordem, o escalonamento de volume e a proteção pós-preenchimento usando os métodos auxiliares do StockSharp (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).

A versão convertida mantém a filosofia do consultor especialista original:

* colocar a primeira ordem para cada direção habilitada em um preço manual ou em um deslocamento automático medido em pips da melhor cotação;
* estender a grade passo a passo usando espaçamento independente para ordens stop e limite;
* escalar volumes de ordens usando um multiplicador que espelha a implementação do MT5;
* armar cada ordem preenchida com seu próprio stop-loss e take-profit, expressos como deslocamentos em pips a partir do preço de entrada;
* aplicar pontos de controle globais de lucro e perda que imediatamente liquidam posições e removem quaisquer ordens pendentes restantes quando atingidos;
* opcionalmente expirar ordens pendentes após um número especificado de minutos.

## Como a Estratégia Funciona
1. **Dados de mercado** – a estratégia assina atualizações do livro de ordens para rastrear o melhor bid/ask e velas (padrão: 1 minuto) para executar manutenção periódica. Nenhum indicador é necessário.
2. **Inicialização da grade** – quando não há posição aberta nem ordem de grade ativa, a estratégia calcula o preço inicial para cada um dos quatro tipos possíveis de ordens:
   * **Buy Stop**: melhor ask + `DeltaFirstBuyStop` (a menos que `FirstBuyStop` seja diferente de zero).
   * **Buy Limit**: melhor bid − `DeltaFirstBuyLimit` (a menos que `FirstBuyLimit` seja diferente de zero).
   * **Sell Stop**: melhor bid − `DeltaFirstSellStop` (a menos que `FirstSellStop` seja diferente de zero).
   * **Sell Limit**: melhor ask + `DeltaFirstSellLimit` (a menos que `FirstSellLimit` seja diferente de zero).
   Cada deslocamento é convertido de pips usando o `PriceStep` do ativo (substituto: 0.0001).
3. **Empilhamento de ordens** – para cada direção habilitada a estratégia cria `CountOrders` entradas separadas por `StepStop` ou `StepLimit` (também em pips). Os volumes seguem a fórmula original: a ordem #0 usa o volume base, enquanto a ordem #N usa `baseVolume * N * coefficient` sempre que o coeficiente for maior que 1. Os volumes são ajustados para respeitar `Security.VolumeStep`, `Security.MinVolume` e `Security.MaxVolume`.
4. **Expiração** – se `ExpirationMinutes` for positivo, a estratégia carimba cada ordem pendente e a cancela automaticamente após o prazo.
5. **Proteção após preenchimento** – quando o StockSharp informa que uma ordem de entrada está concluída, a estratégia registra as ordens de stop-loss e take-profit correspondentes (`StopLoss` e `TakeProfit` em pips). Uma distância zero desabilita a proteção respectiva.
6. **Ponto de controle de lucro** – o PnL realizado mais não realizado é recalculado quando novos dados chegam. Se `ProfitClose` for positivo e atingido, ou `LossClose` (tipicamente negativo) for violado, a estratégia solicita uma liquidação completa: fechamento de mercado da posição, cancelamento de todas as ordens de grade e cancelamento das ordens de proteção restantes. O trading é retomado automaticamente depois que tudo estiver plano.
7. **Manutenção contínua** – cada atualização limpa ordens terminadas, remove itens expirados, tenta colocar uma nova grade quando as condições permitem e evita rearmar durante uma liquidação em andamento.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CountOrders` | Número de ordens por direção habilitada. | 5 |
| `FirstBuyStop`, `FirstBuyLimit`, `FirstSellStop`, `FirstSellLimit` | Preços absolutos opcionais para a primeira ordem em cada direção (0 = usar deslocamento automático). | 0 |
| `DeltaFirstBuyStop`, `DeltaFirstBuyLimit`, `DeltaFirstSellStop`, `DeltaFirstSellLimit` | Deslocamentos em pips aplicados ao melhor bid/ask quando o preço automático é usado. | 15 |
| `UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit` | Habilitar ou desabilitar cada direção de grade. | false |
| `StepStop`, `StepLimit` | Distância entre ordens stop ou limite consecutivas (pips). | 50 |
| `VolumeStop`, `VolumeLimit` | Volume base para a primeira ordem stop/limite. | 0.01 |
| `CoefficientStop`, `CoefficientLimit` | Multiplicador aplicado a ordens adicionais (>1 mantém o comportamento de escalonamento MT5). | 1.6 |
| `ProfitClose` | Limiar de PnL total que aciona a liquidação (unidades monetárias). | 10 |
| `LossClose` | Piso de PnL total que aciona a liquidação (unidades monetárias, tipicamente negativo). | -100 |
| `ExpirationMinutes` | Tempo de vida da ordem pendente em minutos (0 = bom até cancelar). | 60 |
| `StopLoss`, `TakeProfit` | Distâncias em pips para ordens stop/take de proteção criadas após um preenchimento (0 desabilita). | 50 / 0 |
| `CandleType` | Série de velas usada para manutenção periódica. | Velas de 1 minuto |

## Notas de Uso
* Habilite pelo menos um dos quatro interruptores booleanos (`UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit`) para permitir que a grade seja criada.
* A conversão de pips depende do `PriceStep` do ativo. Instrumentos com tamanhos de tick exóticos podem exigir ajustar os deslocamentos para comportamento equivalente.
* `ProfitClose`/`LossClose` comparam a soma do PnL realizado (`Strategy.PnL`) e o PnL não realizado calculado a partir do último melhor bid/ask; certifique-se de que os metadados de preço do passo estejam preenchidos para o instrumento operado.
* As ordens de proteção stop e take são ordens independentes do StockSharp; se você fechar manualmente uma posição fora da estratégia, as ordens de proteção restantes são canceladas quando a posição líquida retorna a zero.
* O parâmetro `CandleType` controla apenas com que frequência a manutenção é executada; a colocação de ordens ainda reage imediatamente às atualizações do livro de ordens.

## Diferenças do Consultor Especialista MT5
* A contabilidade de posições é líquida: o StockSharp mantém uma única posição líquida por ativo, semelhante ao regime de compensação do MT5.
* Em vez dos campos de stop-loss/take-profit incorporados do MT5 em ordens pendentes, as ordens de proteção do StockSharp são criadas apenas após a execução de uma ordem de entrada.
* A normalização de volume usa `Security.VolumeStep`, `MinVolume` e `MaxVolume`; verifique esses valores ao operar CFDs ou exchanges de criptomoedas.
* A estratégia não expõe um botão separado de *fechar tudo* — a rotina de liquidação é totalmente automática através dos limiares de PnL, correspondendo à lógica original do especialista onde `ProfitClose`/`LossClose` acionavam um encerramento completo.

## Primeiros Passos
1. Atribua a estratégia a um conector que forneça pelo menos dados do livro de ordens e velas para o `CandleType` escolhido.
2. Configure os quatro interruptores direcionais e parâmetros de volume para corresponder ao seu perfil de risco.
3. Defina distâncias de stop-loss/take-profit quando ordens de proteção são necessárias (definir como zero para desabilitar).
4. Ajuste `ProfitClose`/`LossClose` a valores consistentes com a moeda da sua conta.
5. Inicie a estratégia; ela aguardará o primeiro snapshot do livro de ordens antes de construir a grade.

> **Versão Python** – não fornecida. Apenas a implementação em C# está incluída, conforme solicitado.

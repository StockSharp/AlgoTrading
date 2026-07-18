# Estratégia Exp Amstell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Exp Amstell** é um sistema de negociação em grade convertido do MetaTrader 4 consultor especialista original `exp_Amstell.mq4`. Ele coloca continuamente ordens de compra e venda de mercado sempre que o preço se afasta de um número configurável de pontos do preenchimento mais recente. Cada negociação individual é gerida de forma independente: uma vez que o mercado se move pela distância de lucro especificada, a estratégia envia uma ordem de compensação para capturar o lucro para essa única camada.

Ao contrário dos sistemas orientados por impulso, o Exp Amstell permanece ativo o tempo todo. Não espera pelas confirmações dos indicadores e, em vez disso, acumula posições em ambos os lados do livro à medida que o mercado oscila. Este comportamento o torna altamente sensível às distâncias dos pontos escolhidos e ao tamanho de cada ordem.

## Lógica de negociação
- **Processamento baseado em ticks.** A estratégia assina cotações de nível 1 e reage a cada mudança no melhor lance e no melhor pedido, assim como a função `start()` no código MQL original.
- **Pilhas longas e curtas independentes.** Ordens de compra são permitidas quando não há negociações longas abertas ou quando o preço de venda caiu pelo menos na distância de reentrada da última entrada longa. As ordens de venda usam a condição simétrica no preço de oferta.
- **Take Profit por negociação.** Cada camada aberta é rastreada separadamente. Quando o bid (para comprados) ou o pedido (para vendidos) avança pelos pontos de take-profit configurados, a estratégia fecha apenas essa camada com uma ordem de mercado. Outras camadas permanecem intocadas.
- **Emulação FIFO.** As negociações executadas são registradas na ordem FIFO para reproduzir a contabilidade baseada em tickets que MetaTrader aplica às posições cobertas. Isto garante que os preenchimentos parciais reduzam primeiro a camada pendente mais antiga.
- **Reconhecimento do portfólio líquido.** StockSharp mantém posições líquidas. Se uma nova ordem de compra compensar uma camada curta aberta, a estratégia removerá essa posição curta de sua pilha sintética antes de registrar o restante como uma nova posição longa.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volume de cada ordem de mercado que abre uma nova camada de grade. |
| `TakeProfitPoints` | `int` | `30` | Distância em MetaTrader pontos que devem ser cobertos pelo preço antes que uma camada individual seja fechada. |
| `ReentryDistancePoints` | `int` | `10` | Distância mínima de ponto da última entrada antes de adicionar outro pedido do mesmo lado. |

A estratégia converte automaticamente os pontos em etapas de preço reais usando o `PriceStep` do instrumento. As cotações de cinco e três dígitos recebem o multiplicador específico de MetaTrader para que `1 point` seja igual a `0.0001` (ou `0.01` para símbolos no estilo JPY).

## Notas de implementação
- Os dados do nível 1 são suficientes; nenhuma assinatura de vela é necessária. A estratégia declara isso substituindo `GetWorkingSecurities()` e solicitando `(Security, DataType.Level1)`.
- `StartProtection()` é invocado durante `OnStarted` para garantir que o corredor feche qualquer posição restante se a estratégia parar inesperadamente.
- Todos os comentários dentro do arquivo C# permanecem em inglês, correspondendo às diretrizes do projeto.
- Como StockSharp usa posições líquidas, a porta não pode manter compras e vendas opostas abertas simultaneamente. Quando ambos os lados negociam ao mesmo tempo, a ordem mais recente nivelará a exposição existente antes de criar uma nova camada.

## Dicas de uso
1. **Calibre as distâncias dos pontos.** Distâncias menores criam grades mais densas que podem ser negociadas em excesso em mercados voláteis. Distâncias maiores reduzem a atividade, mas aumentam o rebaixamento por camada.
2. **Dimensione os pedidos com prudência.** Os sistemas de grade acumulam exposição rapidamente. Teste volumes conservadores no Designer/Backtester antes de mudar para negociação ao vivo.
3. **Considere controles de risco manuais.** O especialista original não tem stop-loss global. Combine a estratégia com proteções em nível de portfólio para limitar o risco.
4. **Monitore a qualidade da execução.** O algoritmo pressupõe que as ordens de mercado sejam preenchidas perto da melhor oferta/venda. A derrapagem afeta diretamente as distâncias de lucro alcançadas.

## Fonte
Convertido de `MQL/9027/exp_Amstell.mq4`.

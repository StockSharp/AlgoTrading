# Estratégia de fim de semana com lucro próximo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de fim de semana Close Profit** automatiza o script MetaTrader *Closeprofitendofweek.mq5*. A estratégia supervisiona o instrumento atribuído e, às sextas-feiras, após um horário limite configurável, sai de todas as posições lucrativas. O objetivo é garantir ganhos flutuantes antes que apareça o risco de gap do fim de semana.

## Comportamento original MQL
O Expert Advisor de origem pesquisou continuamente as posições por meio do manipulador do cronômetro. Sempre que o horário do servidor correspondia à sexta-feira e ao horário de término configurado, ele percorria todas as posições abertas no símbolo negociado. Cada posição com lucro positivo foi fechada através de uma ordem de mercado. Os símbolos criptográficos foram explicitamente excluídos porque são negociados sem fins de semana.

## StockSharp Implementação
A porta C# mantém a mesma lógica de proteção ao usar o API de alto nível do StockSharp:
- Assina uma série de velas configuráveis apenas para receber atualizações regulares.
- Verifica cada vela finalizada e verifica se representa uma sexta-feira cujo horário de fechamento é posterior ao corte definido pelo usuário.
- Acessa o portfólio conectado para avaliar a posição líquida do símbolo estratégico.
- Emite uma ordem de mercado na direção oposta para cada exposição lucrativa que ainda esteja aberta.
- Ignora totalmente a rotina quando o instrumento é classificado como um ativo criptográfico.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `StartTradeTime` | Início da janela de monitoramento (mantida a paridade com as entradas MQL). | `00:00` |
| `EndTradeTime` | Hora do dia de sexta-feira após a qual as posições lucrativas devem ser fechadas. | `20:00` |
| `CloseTradesAtEndTime` | Habilita ou desabilita a rotina de fechamento automático. | `true` |
| `CandleType` | Série de dados usada para rastrear o tempo (o padrão é velas de 1 minuto). | `TimeFrameCandle(1m)` |

## Fluxo de Execução
1. Ao iniciar a estratégia ela verifica se o título selecionado pertence à classe de criptoativos. Os instrumentos criptográficos são ignorados para espelhar a cláusula de guarda MetaTrader.
2. Uma assinatura de vela é criada para receber retornos de chamada regulares assim que cada vela terminar.
3. Cada vela finalizada aciona as verificações do cronograma. Somente as sextas-feiras que fecharam após o horário limite levam a processamento adicional.
4. A estratégia verifica a carteira conectada, filtra a posição que corresponde ao título configurado e lê seu lucro flutuante.
5. Se o lucro flutuante for maior que zero, uma ordem de mercado é enviada no sentido oposto para fechar totalmente a exposição.
6. Ordens de saída duplicadas são evitadas inspecionando os pedidos ativos antes de enviar novos.

## Notas de uso
- Anexe a estratégia a um instrumento não criptográfico junto com a mesma carteira que possui as posições abertas que você deseja supervisionar.
- A estratégia não abre novas negociações; ele gerencia apenas posições existentes.
- O parâmetro `StartTradeTime` existe para paridade de configuração e extensões futuras, mas não é referenciado pela lógica atual.
- Para portfólios com vários símbolos, execute uma instância por instrumento para replicar o escopo de símbolo único do script MetaTrader.

## Limitações
- A detecção de lucro depende do relatório do portfólio do corretor PnL flutuante. Caso a carteira não seja atualizada em tempo real o comando de fechamento poderá atrasar.
- Somente as posições do símbolo de estratégia configurado são fechadas. As exposições realizadas em outros símbolos permanecem intactas.
- A verificação é executada em eventos de fechamento de velas. Selecione um prazo adequadamente curto se precisar de um cronograma mais apertado.

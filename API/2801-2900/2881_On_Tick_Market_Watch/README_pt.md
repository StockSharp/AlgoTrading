# Estratégia de Observação de Mercado em Tick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Observação de Mercado em Tick** replica o comportamento do script MetaTrader `scOnTickMarketWatch.mq5`. O script original escaneia continuamente a lista de Market Watch e gera um evento personalizado sempre que um novo tick chega para qualquer símbolo, imprimindo o preço de compra e informações de spread. Este port em C# converte esse comportamento em uma estratégia de alto nível do StockSharp que ouve atualizações Level1 e registra as informações de tick através do logger de estratégia.

A estratégia é intencionalmente não negociadora. Seu propósito é fornecer diagnósticos ou monitoramento de dados de tick recebidos em múltiplos instrumentos conectados ao mesmo conector. Como depende das assinaturas de dados do StockSharp, a solução é orientada a eventos e não requer atrasos ou loops manuais como a versão MQL.

## Principais recursos
- Monitora o instrumento principal da estratégia e quaisquer instrumentos adicionais definidos em uma lista separada por vírgulas.
- Subscreve dados Level1 para cada instrumento a fim de capturar atualizações de compra/venda.
- Calcula o spread (ask menos bid) quando ambos os lados estão disponíveis e registra informações detalhadas em inglês.
- Espelha o índice do Market Watch mantendo uma ordem interna idêntica à lista especificada pelo usuário.
- Fornece avisos amigáveis quando um símbolo não pode ser resolvido pelo `SecurityProvider` configurado.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `SymbolsList` | `string` | `""` | Lista separada por vírgulas de identificadores de instrumentos adicionais (por exemplo, `AAPL@NASDAQ,MSFT@NASDAQ`) que devem ser observados além do `Strategy.Security` principal. Cada identificador deve existir no `SecurityProvider` atual. |

## Como funciona
1. Durante `OnStarted`, a estratégia resolve todos os símbolos. O `Strategy.Security` principal é sempre adicionado primeiro, seguido por quaisquer símbolos adicionais fornecidos através de `SymbolsList`.
2. Para cada instrumento resolvido, a estratégia chama `SubscribeLevel1` e anexa um callback que recebe atualizações `Level1ChangeMessage`.
3. Cada callback verifica se a atualização contém pelo menos um dos campos de preço relevantes (`LastTradePrice`, `BestBidPrice` ou `BestAskPrice`).
4. O bid é obtido de `BestBidPrice` (ou recorre a `LastTradePrice` se o melhor bid estiver ausente), o ask vem de `BestAskPrice`, e o spread é calculado se ambos os valores estiverem presentes.
5. O logger imprime uma mensagem correspondente ao script original: `New tick on the symbol <id> index in the list=<index> bid=<bid> spread=<spread>`. Quando o ask não está disponível, `spread` é reportado como `n/a`.
6. Se o StockSharp não conseguir encontrar um símbolo solicitado no `SecurityProvider`, uma mensagem de aviso é emitida e o símbolo é ignorado.

## Instruções de uso
1. Atribua o instrumento principal (`Strategy.Security`) através da interface de configuração de estratégia ou em código.
2. Opcionalmente defina o parâmetro `SymbolsList` com identificadores adicionais separados por vírgulas. A ordem determina o índice reportado na saída do log.
3. Conecte a estratégia a uma fonte de dados capaz de entregar informações Level1 para os instrumentos escolhidos.
4. Inicie a estratégia. Ela se subscreverá imediatamente aos dados Level1 e começará a registrar mensagens de tick.
5. Revise o log da estratégia para verificar os dados de mercado recebidos e os spreads calculados.

## Notas e diferenças frente à versão MQL
- A versão StockSharp é totalmente orientada a eventos. Não há loop manual nem chamada `Sleep`; a plataforma invoca callbacks quando os dados chegam.
- `SymbolsTotal(true)` do MQL é emulado preservando a ordem em que os instrumentos são adicionados à lista de observação. O índice reportado começa em zero para o instrumento de estratégia principal.
- Os valores de spread no MetaTrader são inteiros baseados em pontos. No StockSharp o spread é calculado como uma diferença de preço decimal.
- Eventos de gráfico personalizados são substituídos por entradas de log porque as estratégias StockSharp já incluem um subsistema de logging flexível.
- Se um símbolo não tiver preço ask na atualização atual, o spread é reportado como `n/a`, fornecendo clareza sobre informações Level1 incompletas.
- A estratégia é projetada estritamente para monitoramento e não envia nenhuma ordem.

## Exemplo de saída de log
```
New tick on the symbol AAPL@NASDAQ index in the list=0 bid=171.25 spread=0.02
New tick on the symbol MSFT@NASDAQ index in the list=1 bid=324.10 spread=n/a
```
Essas entradas demonstram como as informações de bid e spread são reportadas para cada instrumento rastreado na lista do Market Watch.

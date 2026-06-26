# Estratégia Sensitive MACD com Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é uma conversão direta para StockSharp do consultor especialista MACD "Sensitive" para MetaTrader 5. Combina cruzamentos do MACD com ferramentas de gestão de risco configuráveis (stop loss fixo, take profit e trailing stops baseados em pips). O algoritmo funciona exclusivamente em velas concluídas e usa a API de alto nível para assinar o período desejado.

## Indicadores e Dados
- **MACD (Moving Average Convergence Divergence)** – configurado com comprimentos de EMA rápida, lenta e de sinal independentes.
- **Velas** – período selecionável pelo usuário fornecido através do parâmetro `CandleType`.

## Condições de Entrada
1. Uma nova vela deve fechar para evitar ruído intrabarra.
2. Os valores do MACD são processados a partir da vinculação do indicador:
   - `macd` representa a linha principal do MACD.
   - `signal` é a linha de sinal (EMA da diferença do MACD).
3. Requisitos de **entrada comprada**:
   - O MACD cruza acima da linha de sinal (`macd > signal` enquanto os valores anteriores satisfaziam `macd < signal`).
   - O MACD permanece em território negativo (`macd < 0`).
   - A magnitude absoluta do MACD é maior que `MacdOpenLevel * Point`, garantindo um deslocamento significativo.
   - Não há posição comprada ativa (posição líquida é menor ou igual a zero). Os vendidos existentes são revertidos enviando a quantidade necessária.
4. Os requisitos de **entrada vendida** espelham a configuração comprada:
   - O MACD cruza abaixo da linha de sinal permanecendo positivo.
   - A magnitude absoluta do MACD excede o limiar configurado.
   - Não existe posição vendida aberta (posição líquida é maior ou igual a zero). Os comprados existentes são nivelados antes de abrir o vendido.

## Gestão de Saída
- **Take Profit**: Uma vez aberta a operação, a estratégia armazena um nível alvo definido por `TakeProfitPips`. Se a máxima de uma vela comprada ou a mínima de uma vela vendida atingir este nível, a posição é fechada a mercado.
- **Stop Loss**: Um stop de proteção é calculado a partir de `StopLossPips`. Para comprados, uma queda do preço ao nível de stop aciona uma saída a mercado. Os vendidos reagem a subidas do preço que atingem o stop.
- **Trailing Stop**: Quando `TrailingStopPips` é diferente de zero, o algoritmo ativa uma lógica de trailing depois que o preço avança pelo menos `TrailingStopPips + TrailingStepPips` pips a partir da entrada. Os movimentos subsequentes ajustam o nível de stop mantendo sempre a distância de trailing especificada a partir do último fechamento. O passo de trailing deve ser positivo sempre que o trailing stop estiver habilitado; caso contrário, a estratégia para com uma mensagem de erro.
- Quando não há posição ativa, as variáveis de rastreamento internas são redefinidas para se preparar para a próxima operação.

## Dimensionamento de Posição
As quantidades das ordens são controladas pelo parâmetro de estratégia integrado `Volume` (padrão: 0.1). As reversões adicionam automaticamente o valor absoluto da posição atual ao volume desejado para mudar de direção em uma única ordem de mercado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `FastLength` | Período da EMA rápida utilizado pela linha principal do MACD. | 12 |
| `SlowLength` | Período da EMA lenta utilizado pela linha principal do MACD. | 26 |
| `SignalLength` | Período da EMA de sinal para o MACD. | 9 |
| `MacdOpenLevel` | Magnitude mínima do MACD (em pontos de preço) necessária para acionar operações. | 3 |
| `StopLossPips` | Distância do stop de proteção em pips. | 35 |
| `TakeProfitPips` | Distância de take-profit em pips. | 75 |
| `TrailingStopPips` | Distância de trailing stop em pips (0 desabilita o trailing). | 5 |
| `TrailingStepPips` | Distância adicional que o preço deve percorrer antes de o trailing stop ser atualizado. | 5 |
| `CandleType` | Tipo de vela fonte (período). | Velas de 1 minuto |
| `Volume` | Volume da ordem, expresso em lotes/contratos dependendo do instrumento. | 0.1 |

## Notas Adicionais
- Os valores de pips e pontos do MACD são derivados do passo de preço do instrumento e sua precisão decimal. O código ajusta os símbolos forex de 3 e 5 dígitos escalando o tamanho do pip de acordo.
- Todos os comentários dentro do código fonte estão escritos em inglês, e a implementação usa apenas as APIs de alto nível do StockSharp de acordo com as diretrizes do projeto.
- A estratégia evita intencionalmente o gerenciamento de preenchimentos parciais e assume que as ordens de mercado são preenchidas imediatamente ao executar no simulador ou no trading real. Salvaguardas adicionais podem ser adicionadas se necessário.

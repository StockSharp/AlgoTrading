# Estratégia Poker Show
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Poker Show é uma portação direta do assessor especialista do MetaTrader 5 "Poker_SHOW". Combina um filtro de tendência de média móvel com um gatilho probabilístico que imita o sorteio de uma mão de pôquer. As operações são executadas apenas quando o valor da mão gerada aleatoriamente cai abaixo de um limite configurável de combinação de pôquer. A abordagem produz entradas pouco frequentes enquanto permanece alinhada com a tendência predominante detectada pela média móvel.

A estratégia trabalha em um único símbolo e depende de velas regulares baseadas em tempo. As decisões de trading são avaliadas uma vez por vela completada, o que corresponde ao assessor original que reage na abertura de cada nova barra.

## Lógica principal

1. **Filtro de tendência de média móvel**
   - Uma média móvel configurável (SMA, EMA, SMMA ou LWMA) é calculada a partir da fonte de preço selecionada (fechamento, abertura, máxima, mínima, mediana, típico ou preço ponderado).
   - O indicador pode ser deslocado para frente no tempo para reproduzir o input "shift" do MetaTrader. A estratégia sempre usa o valor da última vela completamente formada, igual ao EA fonte.

2. **Portão de probabilidade**
   - Cada lado (comprado ou vendido) sorteia um valor aleatório independente entre 0 e 32.767 em cada barra.
   - O sorteio é comparado com a combinação de pôquer selecionada. Combinações de rank mais alto (p. ex., royal flush) têm limites numéricos menores e portanto se acionam com menos frequência, enquanto combinações de rank menor (p. ex., um par) operam com mais frequência.

3. **Regras direcionais**
   - Operações compradas exigem que a média móvel permaneça acima do preço pelo menos a distância configurada. Quando a opção **Sinais invertidos** está ativa, a condição é invertida.
   - Operações vendidas exigem que a média móvel permaneça abaixo do preço com a mesma margem, com a condição invertida quando o interruptor de inversão está ativo.
   - Apenas uma posição pode estar ativa de cada vez. Entrar na direção oposta compensa automaticamente qualquer exposição aberta antes de estabelecer o novo trade.

4. **Gestão de risco**
   - Os níveis opcionais de stop loss e take profit são calculados em passos de preço (pontos) relativos ao preço de execução. Definir uma distância como zero desabilita o nível correspondente.
   - Stops e alvos são verificados em cada vela completada. Quando atingidos, a estratégia fecha a posição e redefine os marcadores de risco.

5. **Proteção de posição**
   - O módulo de proteção integrado do StockSharp é ativado na inicialização para preservar a conta de perdas inesperadas durante execuções manuais.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| **Combinação de pôquer** | Limite de probabilidade que o sorteio aleatório deve exceder para permitir um novo trade. Representa mãos clássicas de pôquer, de royal flush (mais rara) a um par (mais comum). |
| **Volume** | Volume de ordem em lotes. Usado tanto para novas entradas quanto para reverter posições existentes. |
| **Stop loss** | Distância entre o preço de entrada e o stop protetor, medida em passos de preço. Definir como zero para desabilitar. |
| **Take profit** | Distância entre o preço de entrada e o alvo de lucro, medida em passos de preço. Definir como zero para desabilitar. |
| **Habilitar compra** | Permite à estratégia abrir posições compradas. |
| **Habilitar venda** | Permite à estratégia abrir posições vendidas. |
| **Distância MA** | Distância mínima em passos de preço entre o valor da média móvel e o preço atual. Atua como filtro de confirmação de tendência. |
| **Período MA** | Número de barras usadas pela média móvel. |
| **Deslocamento MA** | Deslocamento horizontal aplicado à média móvel (em barras), correspondendo ao input `ma_shift` do MetaTrader. |
| **Método MA** | Tipo de suavização da média móvel: simples, exponencial, suavizada ou ponderada linear. |
| **Preço aplicado** | Preço de vela utilizado no cálculo da média móvel. |
| **Sinais invertidos** | Inverte a comparação entre a média móvel e o preço, trocando efetivamente a lógica de comprado e vendido. |
| **Tipo de vela** | Período de tempo da assinatura de velas. O padrão é uma hora para replicar as configurações originais. |

## Notas e recomendações

- O portão de probabilidade torna a estratégia altamente estocástica. Os backtests devem usar múltiplas execuções ou análise de Monte Carlo para entender a distribuição dos resultados.
- Como o gerenciamento de operações depende de velas completadas, picos intrabarra grandes podem ultrapassar os níveis de stop ou alvo antes que a estratégia possa reagir. Considere executar em períodos de tempo menores se esse comportamento for indesejável.
- Para reproduzir fielmente o ambiente do MetaTrader, certifique-se de que o instrumento usa o mesmo tamanho de contrato e passo de preço para que as distâncias baseadas em pontos correspondam aos lotes e valores de pip originais.
- A estratégia usa ordens a mercado (`BuyMarket` e `SellMarket`) como no assessor especialista fonte. O tratamento de slippage é delegado à infraestrutura de execução do StockSharp.

# Estratégia de Múltiplos Indicadores SSB5_123
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port StockSharp do consultor especialista MetaTrader 5 "ssb5_123". O código original vem da coleção SSB (Step by Step) de Yury V. Reshetov e combina vários osciladores clássicos para confirmar rompimentos direcionais. A versão StockSharp mantém a mesma lógica usando a API de assinatura de velas de alto nível e implementações de indicadores nativos.

O algoritmo trabalha exclusivamente com velas concluídas. Compara o preço de abertura da barra atual com a barra anterior, verifica o momentum e a direção do Awesome Oscillator, MACD e histograma OsMA, e verifica se o preço está sendo negociado acima ou abaixo de uma média móvel suavizada. Confirmação adicional é obtida do oscilador estocástico exigindo que tanto %K quanto %D estejam acima ou abaixo do nível 50.

## Indicadores e Sinais
Os seguintes indicadores são empregados exatamente como na versão MetaTrader:

- **Média Móvel Suavizada (SMMA)**: média móvel suavizada de 45 períodos calculada a partir das aberturas de velas. A direção de entrada requer que o preço de abertura esteja no lado correto da média.
- **MACD (rápido 47, lento 95, sinal 74)**: a linha principal deve ser positiva para trades longos (negativa para trades curtos) e deve estar subindo (caindo) em comparação com a vela anterior.
- **Histograma OsMA**: calculado como MACD menos sua linha de sinal. O histograma deve diminuir para trades longos e aumentar para trades curtos, espelhando a função original `fosma1()`.
- **Awesome Oscillator**: usa as médias móveis suavizadas padrão 5/34 do preço mediano. O valor do oscilador deve ser positivo para longos (negativo para curtos) e seu momentum entre as últimas duas barras deve apontar na direção do trade.
- **Oscilador Estocástico (K=25, D=12, Slowing=56)**: tanto as linhas %K quanto %D devem estar acima de 50 para trades longos e abaixo de 50 para trades curtos, fornecendo um filtro de regime.

## Lógica de Negociação
1. Aguardar uma nova vela concluída.
2. Avaliar a **configuração longa**. Todas as seguintes condições devem ser verdadeiras:
   - A abertura da vela atual é menor ou igual à abertura da vela anterior.
   - O Awesome Oscillator é positivo e está caindo em relação ao valor anterior.
   - A linha principal MACD é positiva e está subindo em relação ao valor anterior.
   - O histograma OsMA não está aumentando (histograma atual menos histograma anterior é menor ou igual a zero).
   - A abertura da vela atual está acima da média móvel suavizada.
   - As linhas estocásticas %K e %D estão em ou acima de 50.
3. Avaliar a **configuração curta**. Todas as seguintes condições devem ser verdadeiras:
   - A abertura da vela atual é maior ou igual à abertura da vela anterior.
   - O Awesome Oscillator é negativo e está subindo em relação ao valor anterior.
   - A linha principal MACD é negativa e está caindo em relação ao valor anterior.
   - O histograma OsMA não está diminuindo (histograma atual menos histograma anterior é maior ou igual a zero).
   - A abertura da vela atual está abaixo da média móvel suavizada.
   - As linhas estocásticas %K e %D estão em ou abaixo de 50.
4. Se uma posição já existir, um sinal oposto a fecha imediatamente, replicando o gerenciamento de ordens original do MetaTrader.
5. Quando plano, uma entrada longa tem prioridade: se ambos os sinais forem verdadeiros (possível quando todos os indicadores são exatamente zero), a estratégia abre uma posição longa. Caso contrário, abre uma posição curta quando apenas as condições curtas são satisfeitas.

## Parâmetros
- **SMMA Period** – comprimento do filtro de média móvel suavizada (padrão 45).
- **MACD Fast / Slow / Signal** – períodos EMA para o indicador MACD (47 / 95 / 74).
- **Stochastic %K / %D / Slowing** – período principal, período de suavização e suavização adicional para o oscilador estocástico (25 / 12 / 56).
- **Order Volume** – quantidade usada para ordens de mercado (padrão 1).
- **Candle Type** – período das velas de entrada (padrão 1 hora). Ajuste para corresponder ao período usado no MetaTrader.

## Notas de Uso
- A estratégia opera apenas em velas terminadas; atualizações intrabarra são ignoradas.
- Os valores de indicadores da vela anterior são armazenados em cache para que as comparações de momentum correspondam ao comportamento exato das funções auxiliares originais `fao1`, `fmacd1` e `fosma1`.
- Não há regras integradas de stop-loss ou take-profit no consultor especialista original. O gerenciamento de risco deve ser adicionado externamente se necessário.
- As configurações padrão de indicadores correspondem aos parâmetros MQL fornecidos, mas todos os valores são expostos como objetos `StrategyParam` e podem ser otimizados através do otimizador StockSharp.

## Considerações de Conversão
- A versão MetaTrader usa um número mágico e validação manual de volume; essas partes não são necessárias no StockSharp e foram omitidas.
- A lógica de fechamento de ordens segue a mesma precedência que o script MQL: as posições são fechadas primeiro, e novas entradas só são feitas quando a estratégia está plana.
- As implementações do Awesome Oscillator e MACD vêm da biblioteca de indicadores StockSharp, eliminando a necessidade de manipulação manual de buffer presente no código original.

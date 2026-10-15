# Estratégia de 555 Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia 555 Scalper é uma conversão direta do consultor especialista "555 Scalper" de MetaTrader. Opera em qualquer período primário enquanto depende de filtros de períodos superiores e confirmação de momentum mensal. O algoritmo combina um cruzamento de médias móveis ponderadas lineares (LWMA) rápida/lenta com uma confirmação de momentum em período superior e um filtro MACD mensal. A lógica de proteção espelha o EA original, incluindo movimentos de break-even, trailing clássico baseado em pips, stops de emergência baseados em equity e saídas baseadas em dinheiro.

## Lógica de trading
- **Filtro de tendência:** Calcula uma LWMA rápida e uma lenta sobre o preço típico do período de trading. Posições compradas exigem que a LWMA rápida fique acima da lenta; posições vendidas exigem o oposto.
- **Estrutura de velas:** Valida que as duas últimas velas completadas se sobreponham (mínima de duas barras atrás abaixo da máxima anterior para compradas, e vice-versa para vendidas) para aproximar a confirmação estilo fractal usada pelo EA.
- **Filtro de momentum:** Usa um indicador Momentum de 14 períodos calculado em um período superior derivado do período de trading (ex., M1 → M15, M5 → M30, M15 → H1, etc.). Uma operação só é válida se pelo menos uma das três últimas leituras de momentum se desviar do nível neutro 100 pelo limiar configurado (0.3 por padrão).
- **Confirmação MACD:** Aplica um filtro MACD mensal (12/26/9) e só compra quando a linha principal do MACD está acima da linha de sinal, ou vende quando está abaixo.
- **Dimensionamento de posição:** Começa de um lote base e multiplica cada entrada adicional pelo expoente de lote configurado, habilitando pirâmide controlada até o número máximo de operações por direção.

## Gestão de risco
- **Stop-loss e take-profit iniciais:** Cada nova posição recebe um stop-loss e take-profit inicial baseados em distâncias de pip estilo MetaTrader.
- **Movimento break-even:** Quando o preço avança um número configurável de pips no lucro, o stop é movido para break-even mais um offset.
- **Trailing stop:** Implementa a lógica de trailing de pip original deslocando o stop com o preço uma vez que a operação está no lucro.
- **Alvos de dinheiro:** Take-profits opcionais de dinheiro e porcentagem fecham a posição quando o lucro flutuante atinge os limites configurados.
- **Trailing de dinheiro:** Rastreia o lucro flutuante máximo e sai se o lucro recuar um valor configurável após atingir o nível de ativação.
- **Stop de equity:** Monitora o equity máximo da conta alcançado durante a sessão e liquida todas as posições se o drawdown flutuante exceder a porcentagem permitida.

## Parâmetros
- **BaseVolume / LotExponent:** Define o tamanho inicial da operação e o multiplicador para entradas adicionais.
- **StopLossSteps / TakeProfitSteps:** Distâncias em pips para os níveis de proteção.
- **FastMaPeriod / SlowMaPeriod:** Períodos da LWMA rápida e lenta do filtro de tendência.
- **Limiares de momentum:** Desvio necessário de 100 para configurações compradas e vendidas.
- **MaxTrades:** Número máximo de entradas escalonadas por direção.
- **Configurações de BreakEven e Trailing:** Configura o gatilho de break-even baseado em pip, o offset e a distância de trailing.
- **Gestão de dinheiro:** Habilita ou desabilita o take-profit em dinheiro, o take-profit em porcentagem e os controles de trailing de dinheiro.
- **Stop de equity:** Porcentagem de drawdown a partir do pico de equity que aciona uma saída global.

## Notas de uso
1. Anexe a estratégia a qualquer instrumento e selecione o período de trading desejado através do parâmetro `CandleType`.
2. A fonte de momentum do período superior é calculada automaticamente com base no período primário; certifique-se de que dados históricos para ambos os períodos estejam disponíveis.
3. A fonte de MACD mensal requer dados de velas mensais. Ao testar, forneça histórico suficiente para aquecer o sinal MACD.
4. Ajuste o volume, as distâncias em pips e os limites de gestão de dinheiro de acordo com a volatilidade do instrumento e o perfil de risco da conta.

A estratégia reproduz o processo de decisão central do EA original enquanto aproveita a API de alto nível do StockSharp para assinaturas de dados, gerenciamento de indicadores e execução de ordens.

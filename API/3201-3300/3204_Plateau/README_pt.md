# Estratégia de Plateau
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Plateau é uma conversão do consultor especialista original de MetaTrader 5. Combina um par de médias móvias ponderadas lineares com Bollinger Bands para detectar possíveis reversões quando o preço negocia próximo à banda inferior.

## Ideia de trading

* Calcular médias móvias rápidas e lentas usando o método de suavização selecionado e a fonte de preço.
* Construir Bollinger Bands em torno da mesma série de preços.
* Quando a média rápida cruza acima da lenta enquanto a vela anterior fechou abaixo da banda inferior, abrir uma posição comprada.
* Quando a média rápida cruza abaixo da lenta enquanto a vela anterior fechou acima da banda inferior, abrir uma posição vendida.
* Opcionalmente inverter os sinais se o interruptor `Reverse` estiver habilitado.

## Gestão de ordens

* As posições podem ser dimensionadas com um lote fixo ou arriscando uma porcentagem do valor do portfólio por operação.
* Os níveis de stop-loss e take-profit são expressos em pips e anexados imediatamente após o preenchimento da ordem de mercado.
* Um trailing stop pode ser ativado quando tanto a distância de trailing quanto o passo são positivos.
* Quando `Close Opposite` está habilitado, a estratégia fecha automaticamente a posição oposta antes de entrar em uma nova operação.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| Stop Loss | Distância do stop-loss em pips. |
| Take Profit | Distância do take-profit em pips. |
| Trailing Stop | Distância do trailing stop em pips. |
| Trailing Step | Incremento mínimo (em pips) necessário para mover o trailing stop. |
| Money Mode | Escolher entre lote fixo e dimensionamento por porcentagem de risco. |
| Lot / Risk | O tamanho de lote fixo ou a porcentagem de risco dependendo do modo de dinheiro selecionado. |
| Fast MA / Slow MA | Períodos para o par de médias móvias. |
| MA Shift | Deslocamento horizontal aplicado a ambas as médias móvias. |
| MA Method | Algoritmo de suavização da média móvia. |
| MA Price | Fonte de preço usada para os cálculos de média móvia. |
| Bands Period | Período de média para Bollinger Bands. |
| Bands Shift | Deslocamento horizontal aplicado aos valores de Bollinger Bands. |
| Bands Deviation | Multiplicador de desvio padrão para Bollinger Bands. |
| Bands Price | Fonte de preço usada para os cálculos de Bollinger Bands. |
| Reverse | Inverter a lógica de sinal comprado e vendido. |
| Close Opposite | Fechar uma posição existente na direção oposta antes de abrir uma nova. |
| Verbose Log | Imprimir informações detalhadas de execução no log. |
| Candle Type | Série de dados de velas usada para cálculos de indicadores. |

## Notas

* O tamanho do pip é automaticamente ajustado para instrumentos com três ou cinco dígitos decimais para corresponder ao comportamento do especialista original.
* Quando o trailing stop está habilitado, o passo de trailing deve ser estritamente positivo; caso contrário, a estratégia lança um erro na inicialização.
* O dimensionamento de posição baseado em risco requer tanto uma distância de stop-loss válida quanto dados de valoração do portfólio. Quando não disponíveis, a estratégia recorre ao volume padrão.

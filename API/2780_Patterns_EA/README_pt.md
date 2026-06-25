# Estratégia de Padrões EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Padrões EA é um sistema de ação de preço que escaneia as três velas concluídas mais recentes em busca de uma ampla gama de formações de uma, duas e três barras. A lógica é um port StockSharp do expert advisor original MQL5 "Patterns_EA" e preserva seu catálogo configurável de 30 configurações de candlestick. Cada padrão pode ser habilitado ou desabilitado de forma independente e pode ser atribuído à execução comprada ou vendida, permitindo que a estratégia imite as regras discricionárias do script fonte.

## Grupos de padrões
O detector avalia a vela atual e até as duas velas anteriores dependendo do grupo de padrões:

- **Grupo 1 – Padrões de uma barra:** Neutral Bar, Force Bar Up, Force Bar Down, Hammer, Shooting Star.
- **Grupo 2 – Padrões de duas barras:** Inside, Outside, Double Bar High Lower Close, Double Bar Low Higher Close, Mirror Bar, Bearish Harami, Bearish Harami Cross, Bullish Harami, Bullish Harami Cross, Dark Cloud Cover, Doji Star, Engulfing Bearish Line, Engulfing Bullish Line, Two Neutral Bars.
- **Grupo 3 – Padrões de três barras:** Double Inside, Pin Up, Pin Down, Pivot Point Reversal Up, Pivot Point Reversal Down, Close Price Reversal Up, Close Price Reversal Down, Evening Star, Morning Star, Evening Doji Star, Morning Doji Star.

Um parâmetro de tolerância (Equality Pips) controla o quão próximos dois preços devem coincidir para satisfazer as verificações de igualdade, reproduzindo a configuração de "distância máxima em pips" do EA original.

## Parâmetros
- **Candle Type** – Período utilizado para a detecção de padrões.
- **Opened Mode** – Lógica de gestão de posição (Any, Swing, Buy One, Buy Many, Sell One, Sell Many) replicada da versão MQL.
- **Equality Pips** – Distância em pips que define a igualdade de preços; ajustada pelo passo de preço do instrumento.
- **Enable One-Bar Patterns / Enable Two-Bar Patterns / Enable Three-Bar Patterns** – Interruptores para cada grupo de padrões.
- **Enable {Pattern}** – Interruptores individuais para todas as 30 formações.
- **{Pattern} Order** – Direção da operação (compra ou venda) atribuída ao padrão correspondente.

Todos os parâmetros são expostos através de objetos `StrategyParam`, permitindo otimização ou vinculação de UI quando usado dentro de aplicações StockSharp.

## Lógica de trading
1. A estratégia se inscreve na série de velas configurada e aguarda velas concluídas.
2. Quando uma nova barra fecha, as últimas três velas são armazenadas em cache e o detector avalia os grupos de padrões habilitados.
3. Cada padrão replica as regras condicionais da fonte MQL5, incluindo comparações tolerantes e relações de sombra/corpo.
4. Quando um padrão é confirmado, `TriggerPattern` verifica se o grupo e o padrão individual estão habilitados, verifica a direção selecionada e aplica o modo de posição configurado.
5. A estratégia envia uma ordem de mercado na direção atribuída. No modo Swing, primeiro fecha qualquer posição aberta na direção oposta.

## Modos de posição
- **Any:** Permite sinais em ambas as direções sem restrições adicionais.
- **Swing:** Mantém uma única posição líquida; sinais opostos eliminam a posição existente antes de entrar na nova.
- **Buy One / Sell One:** Restringem as operações a uma única posição comprada ou vendida respectivamente.
- **Buy Many / Sell Many:** Permitem múltiplas entradas na direção especificada enquanto ignoram sinais na direção oposta.

## Notas
- O algoritmo usa `Security.PriceStep` para traduzir a tolerância de igualdade em distância de preço absoluta. Se o instrumento não definir um passo de preço, um padrão de 0.0001 é aplicado.
- Nenhum indicador adicional é necessário; toda a lógica depende unicamente da geometria das velas, correspondendo à intenção do expert advisor original.

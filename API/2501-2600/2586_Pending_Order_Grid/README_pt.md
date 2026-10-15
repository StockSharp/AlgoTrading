# Estratégia de Grade de Ordens Pendentes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento do consultor especialista de grade de ordens pendentes "AntiFragile" do MetaTrader. Ela constrói continuamente uma grade simétrica de ordens de stop em torno do preço de mercado atual e aplica saídas protetoras quando as posições são abertas.

## Lógica central
- Na inicialização, a estratégia armazena em cache o melhor bid e ask de dados de nível 1 / livro de ordens e coloca ordens de compra-stop acima do preço e ordens de venda-stop abaixo do preço.
- Os preços das ordens são deslocados do mercado pelo parâmetro *Distance* e cada nível subsequente é espaçado por *Spacing (ticks)* multiplicado pelo passo de preço do instrumento.
- Cada novo nível de grade aumenta o volume da ordem em *Volume Increase %* em relação ao tamanho inicial, implementando o escalonamento estilo martingale da versão MQL.
- Quando uma ordem é executada, a posição líquida resultante é protegida com ordens de stop-loss e take-profit. A lógica de trailing stop opcional reutiliza o último bid/ask para ajustar o stop quando o lucro não realizado excede a distância de trailing.
- A grade é reconstruída automaticamente depois que todas as ordens pendentes foram executadas ou canceladas e a posição retorna ao plano.

## Parâmetros
- **Starting Volume** – tamanho de lote/contrato para a primeira ordem pendente. Ordens subsequentes escalam por *Volume Increase %*.
- **Volume Increase %** – incremento percentual adicionado a cada novo nível de grade (0,1 equivale a +0,1% por nível).
- **Distance** – deslocamento de preço absoluto adicionado antes da primeira ordem (interpretado em moeda do instrumento).
- **Spacing (ticks)** – número de passos de preço entre ordens de grade consecutivas.
- **Orders per side** – número máximo de ordens de grade para comprados e vendidos separadamente.
- **Take Profit (ticks)** – distância do alvo de lucro desde a entrada média, expressa em passos de preço.
- **Stop Loss (ticks)** – distância do stop desde a entrada média. Definir como zero para desabilitar o stop inicial.
- **Trailing Stop (ticks)** – distância de trailing. Definir como zero para desabilitar os ajustes de trailing.
- **Enable Long Grid / Enable Short Grid** – interruptores para colocar ordens buy-stop ou sell-stop.

## Notas de implementação
- As estratégias do StockSharp usam posições líquidas, portanto execuções opostas se compensarão mutuamente em vez de criar cestas cobertas como no MT4. A grade permanece simétrica, mas apenas a exposição líquida é rastreada.
- Os volumes e preços são arredondados para os tamanhos de passo do instrumento antes de enviar as ordens.
- Os trailing stops são recriados cancelando a ordem de stop anterior e enviando uma nova em um nível mais ajustado quando o lucro excede a distância de trailing.
- A estratégia requer dados do livro de ordens (SubscribeOrderBook) para impulsionar o rastreamento de preço e a lógica de trailing.

## Dicas de uso
1. Configure *Starting Volume* e *Volume Increase %* de forma conservadora; os padrões originais pressupõem o dimensionamento de lote Forex e podem crescer rapidamente.
2. Certifique-se de que o portfólio suporte ordens de stop para o local de destino. Todas as entradas de grade são ordens stop-market.
3. Monitore os requisitos de margem porque um grande número de ordens pendentes pode consumir capital reservado.

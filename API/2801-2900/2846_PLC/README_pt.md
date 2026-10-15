# Estratégia PLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia PLC replica o comportamento do consultor especialista MetaTrader `PLC (barabashkakvn's edition)` usando a API de alto nível do StockSharp. O algoritmo opera no período alto especificado pelo parâmetro `Entry Timeframe` e coloca ordens stop de rompimento acima e abaixo do candle finalizado mais recente. Fractais de períodos mais baixos (M5 e H1 por padrão) são usados para escalar dinamicamente o volume da ordem. Uma vez que o lucro flutuante de todas as posições abertas excede o limiar configurado, a estratégia liquida toda a posição e aguarda o próximo setup.

## Lógica de trading

1. **Processamento de novo candle** – a estratégia reage apenas quando um candle está completamente fechado no período principal. Todos os cálculos são realizados com os dados da barra fechada para evitar repintagem.
2. **Manutenção de ordens/posição** – antes de avaliar um novo setup, o algoritmo cancela ordens stop pendentes programadas para exclusão e fecha posições quando o objetivo de lucro foi alcançado em uma barra anterior.
3. **Deslocamentos de preço** – o máximo e mínimo do último candle finalizado são deslocados pelo número de pips configurados via `Shift OHLC`. O tamanho de pip é ajustado automaticamente para símbolos forex de 3 ou 5 dígitos.
4. **Atualizações de fractais** – assinaturas dedicadas rastreiam padrões de fractais nos períodos M5 e H1. Os valores do fractal ascendente e descendente mais recentes são armazenados quando um padrão clássico de cinco barras é concluído.
5. **Verificação de distância** – uma nova compra stop é colocada apenas se o máximo deslocado estiver pelo menos `Shift Position` pips acima do preço de entrada mais alto das negociações compradas abertas, ou se não houver negociações compradas e nenhum buy stop ativo. A mesma regra com comparações invertidas se aplica às vendas stop.
6. **Dimensionamento dinâmico de lotes** – o volume base (`Buy Volume` ou `Sell Volume`) é multiplicado pelo multiplicador M5 ou H1 quando o nível de stop rompe acima do fractal correspondente. Definir um multiplicador como zero desabilita o escalonamento para esse período.
7. **Registro de ordens** – as ordens stop são enviadas via `BuyStop`/`SellStop`. Referências às ordens registradas são rastreadas para simplificar o cancelamento posterior.
8. **Supervisão de lucros** – após somar o lucro aberto de todos os lotes comprados e vendidos (usando o valor de passo do instrumento), a estratégia ativa o modo de `fechar posições` assim que o lucro excede o `Minimum Profit`. Ordens a mercado são usadas na próxima barra para zerar a exposição.
9. **Feedback de negociações** – quando uma ordem stop pendente é executada, todas as outras stops pendentes são canceladas para imitar a lógica MQL original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Shift OHLC` | Número de pips adicionados acima da máxima do último candle e abaixo da mínima do último candle para determinar os níveis de ativação do stop. |
| `Minimum Profit` | Lucro (na moeda do instrumento) que desencadeia o fechamento de todas as posições abertas. |
| `Shift Position` | Distância mínima em pips entre o novo nível de stop e o preço de abertura extremo das posições existentes. Previne o empilhamento de ordens muito próximo de entradas anteriores. |
| `Buy Volume` / `Sell Volume` | Tamanho base da ordem (lotes). Usado antes de aplicar multiplicadores de fractais. |
| `M5 Multiplier` / `H1 Multiplier` | Multiplicadores de volume ativados quando o preço stop está acima (para comprados) ou abaixo (para vendidos) do fractal mais recente no período respectivo. Use `0` para desabilitar o escalonamento. |
| `Entry Timeframe` | Período principal usado para gerar entradas. Cada candle finalizado neste período desencadeia uma nova avaliação. |
| `M5 Fractal Timeframe` | Período que alimenta o detector de fractais inferior (padrão 5 minutos). |
| `H1 Fractal Timeframe` | Período que alimenta o detector de fractais superior (padrão 1 hora). |

## Gerenciamento de posição

- **Cancelamento** – A estratégia mantém referências a todas as ordens stop pendentes. Quando uma ordem stop é preenchida, todas as ordens pendentes restantes são canceladas no próximo ciclo de avaliação.
- **Zeramento** – Quando `Minimum Profit` é excedido, a posição líquida é zerada usando ordens a mercado (`SellMarket` para comprados, `BuyMarket` para vendidos). A bandeira é limpa assim que o tamanho da posição retorna a zero.
- **Rastreamento de inventário** – As ordens preenchidas são registradas como lotes individuais para replicar o comportamento do MetaTrader que diferencia entre os preços de entrada de compra mais altos e de venda mais baixos.

## Notas

- Os parâmetros padrão espelham a configuração original do consultor especialista. Você pode trocar os períodos de fractais editando os parâmetros `M5 Fractal Timeframe` e `H1 Fractal Timeframe` se o instrumento exigir janelas de contexto diferentes.
- Os volumes são arredondados para baixo até o passo de volume do exchange antes de enviar ordens. Se o valor resultante for zero, a ordem é ignorada.
- O cálculo de lucros usa o valor de preço e passo do instrumento para permanecer compatível com instrumentos que têm valor de tick não unitário.

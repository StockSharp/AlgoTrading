# Espalhar estratégia de coleta de dados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Coletor de Dados Espalhados** é uma porta StockSharp do utilitário MetaTrader 5 "Coletor de dados espalhados" (MQL entrada 33314). O consultor especialista original não faz pedidos; em vez disso, ele escuta o fluxo de compra/venda e conta quantos ticks estão dentro de intervalos de spread predefinidos. Sempre que o ano comercial muda ou o especialista para, ele imprime um resumo estatístico. Esta versão C# reproduz o mesmo comportamento usando o `SubscribeLevel1()` API de alto nível e expõe os limites do intervalo como parâmetros configuráveis.

## Detalhes da operação
- A estratégia assina atualizações de nível 1 (bid/ask) do `Security` principal quando é iniciada.
- Cada vez que os preços de compra e venda estão disponíveis, a estratégia calcula o spread e o converte em unidades de preço multiplicando os limites de pontos configurados por `Security.PriceStep`.
- Seis contadores são mantidos:
  1. Spread estritamente abaixo do primeiro limite.
  2. Espalhe entre o primeiro e o segundo limites.
  3. Espalhe entre o segundo e o terceiro limites.
  4. Espalhe entre o terceiro e o quarto limites.
  5. Espalhe entre o quarto e o quinto limiares.
  6. Spread acima do quinto limite.
- As transições de ano são detectadas a partir do carimbo de data/hora da troca (`Level1ChangeMessage.ServerTime`). Quando o ano muda, a estratégia imprime o resumo do ano finalizado e zera os contadores.
- Quando a estratégia para, ela imprime as estatísticas do ano em curso antes de encerrar.

A porta mantém a natureza apenas de registro do utilitário MQL, permitindo que os traders analisem como os spreads se comportaram durante diferentes períodos sem enviar ordens ou manipular posições.

## Parâmetros
Todas as entradas são expressas em **pontos** (terminologia MetaTrader). A distância do preço real é calculada como `points × Security.PriceStep`.

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `FirstBucketPoints` | 10 | Limite superior do primeiro balde de distribuição. Os spreads estritamente abaixo deste limite são contabilizados na primeira categoria. |
| `SecondBucketPoints` | 20 | Limite superior do segundo balde de distribuição. Os spreads em `[FirstBucketPoints, SecondBucketPoints)` são contados aqui. |
| `ThirdBucketPoints` | 30 | Limite superior do terceiro balde de distribuição. Spreads em `[SecondBucketPoints, ThirdBucketPoints)` aumentam este contador. |
| `FourthBucketPoints` | 40 | Limite superior do quarto balde de spread. Os spreads em `[ThirdBucketPoints, FourthBucketPoints)` são registrados aqui. |
| `FifthBucketPoints` | 50 | Limite superior do quinto balde de spread. Spreads em `[FourthBucketPoints, FifthBucketPoints)` aumentam este contador. |

Todos os limites devem ser estritamente crescentes. A tentativa de iniciar a estratégia com valores `Security.PriceStep` inválidos ou não positivos resulta em uma exceção de tempo de execução, que protege o usuário de estatísticas inconsistentes.

## Registros e saídas
As estatísticas são impressas por meio de `AddInfoLog` no seguinte formato:

```
Ano=2024 Spread<=10pts=15342 Spread_10_20pts=2841 Spread_20_30pts=912 ... Spread>50pts=37
```

Esta saída reflete as declarações `Print` do especialista MetaTrader, facilitando a comparação dos dois ambientes. Use o visualizador de registros StockSharp ou redirecione os registros para um arquivo para análise adicional.

## Lista de verificação de uso
1. Atribua o instrumento alvo a `Strategy.Security` e certifique-se de que seu `PriceStep` corresponda ao tamanho de ponto MetaTrader (para a maioria dos símbolos Forex isso é igual a 0,0001).
2. Ajuste os limites do intervalo se precisar de intervalos de spread diferentes. Mantenha os valores estritamente crescentes.
3. Inicie a estratégia e deixe-a funcionar. Nenhum pedido será enviado.
4. Revise os logs anuais para entender o comportamento de propagação entre as sessões.

A estratégia é intencionalmente leve e segura para ser executada em conjunto com sistemas de negociação ao vivo. Ele ajuda as mesas a construir distribuições históricas de spreads, validar suposições de liquidez e monitorar as condições das corretoras durante longos períodos.

# Estratégia PowerHouse SwiftEdge AI v2.10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento baseada em pivôs com filtro de momentum e gerenciamento de risco simples.
A entrada comprada ocorre quando o preço rompe o máximo de pivô recente com momentum suficiente e distância mínima do sinal anterior.
A entrada vendida é acionada em uma quebra do mínimo de pivô com momentum negativo.
As posições usam take-profit e stop-loss especificados em pontos.

## Cálculo e execução

Este é um exemplo determinístico; o título histórico não implica um modelo de IA treinado. C# e Python compartilham esta interpretação explícita da descrição acima.

Use velas base concluídas. Um máximo pivô supera estritamente as máximas de `PivotPeriod` barras vizinhas de cada lado; um mínimo é estritamente inferior às mínimas. Igualdade exclui o pivô. A confirmação ocorre apenas após concluir as barras da direita, sem sinais retroativos. Guarde separadamente o último máximo e mínimo confirmados.

Compra exige `Close anterior <= máximo pivô` e `Close atual > máximo pivô`; venda exige o cruzamento inverso do mínimo pivô. Momentum é `Close atual - Close de MomentumPeriod barras atrás`: estritamente acima de `MomentumThreshold` para compra ou abaixo do seu negativo para venda. Ambos os lados exigem pelo menos `MinSignalBars` barras concluídas desde a última entrada aceita; zero desativa o intervalo. Sinais rejeitados não são adiados. Não há filtro EMA nem espera de seis horas.

Entre com `Volume` sem posição ou reverta completamente com `Volume + abs(Position)`. Uma ordem pendente bloqueia novas ordens. As distâncias são unidades absolutas do preço cotado: um ponto equivale a uma diferença de 1, não a tick, multiplicador de `PriceStep` ou percentual. Ajuste ao instrumento. A base é a execução real, ponderada por volume em aumentos parciais do mesmo lado; reduzir preserva a base e reverter inicia uma nova.

A proteção local fecha toda a posição a mercado quando o bid de compras ou ask de vendas atinge stop ou take. Fechamentos das velas também verificam a proteção; toques nas máximas/mínimas não simulam eventos intrabar ordenados. A proteção independe do intervalo entre entradas e tem prioridade sobre uma nova entrada na mesma vela. Em lacunas, a execução pode diferir do disparador.

Padrões: `PivotPeriod=2`, `MomentumPeriod=10`, `MomentumThreshold=0`, `MinSignalBars=5`, `StopLossPoints=50`, `TakeProfitPoints=100`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. Períodos e distâncias de saída positivos; limiar e intervalo não negativos.
